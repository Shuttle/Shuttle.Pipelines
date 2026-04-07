using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;

namespace Shuttle.Core.Pipelines;

public class Pipeline : IPipeline
{
    private static readonly Type PipelineObserverType = typeof(IPipelineObserver<>);
    private static readonly Type PipelineContextType = typeof(IPipelineContext<>);

    private readonly Type _abortPipelineType = typeof(AbortPipeline);
    private readonly Dictionary<Type, List<ObserverDelegate>> _delegates = new();

    private readonly ILogger<Pipeline> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<Type, List<PipelineObserverMethodInvoker>> _observerMethodInvokers = new();
    private readonly Dictionary<Type, PipelineContextConstructorInvoker> _pipelineContextConstructors = new();

    private readonly PipelineEventArgs _pipelineEventArgs;
    private readonly Type _pipelineFailedType = typeof(PipelineFailed);
    private readonly PipelineOptions _pipelineOptions;

    private readonly string _raisingPipelineEvent = Resources.VerboseRaisingPipelineEvent;

    protected List<IPipelineStage> Stages = [];

    public Pipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider, ILogger<Pipeline>? logger = null)
    {
        _logger = logger ?? NullLogger<Pipeline>.Instance;
        _pipelineOptions = Guard.AgainstNull(Guard.AgainstNull(pipelineOptions).Value);
        ServiceProvider = Guard.AgainstNull(serviceProvider);

        _pipelineEventArgs = new(this);
    }

    public IServiceProvider ServiceProvider { get; }

    public Guid Id { get; } = Guid.NewGuid();
    public bool ExceptionHandled { get; internal set; }
    public Exception? Exception { get; internal set; }
    public bool Aborted { get; internal set; }
    public string StageName { get; private set; } = string.Empty;
    public Type? EventType { get; private set; }

    public IState State { get; } = new State();

    public IPipeline AddObserver(IPipelineObserver pipelineObserver, ObserverPosition position = ObserverPosition.Anywhere)
    {
        return AddObserver(new InstancePipelineObserverProvider(pipelineObserver), position);
    }

    public IPipeline AddObserver(Type observerType, ObserverPosition position = ObserverPosition.Anywhere)
    {
        return AddObserver(new ServiceProviderPipelineObserverProvider(ServiceProvider, Guard.AgainstNull(observerType)), position);
    }

    public IPipeline AddObserver<T>(ObserverPosition position = ObserverPosition.Anywhere)
    {
        return AddObserver(typeof(T), position);
    }

    public IPipeline AddObserver<TDelegate>(TDelegate handler, ObserverPosition position = ObserverPosition.Anywhere) where TDelegate : Delegate
    {
        if (!typeof(Task).IsAssignableFrom(Guard.AgainstNull(handler).Method.ReturnType))
        {
            throw new ApplicationException(Resources.AsyncDelegateRequiredException);
        }

        var parameters = handler.Method.GetParameters();
        Type? eventType = null;

        foreach (var parameter in parameters)
        {
            var parameterType = parameter.ParameterType;

            if (parameterType.IsCastableTo(PipelineContextType))
            {
                eventType = parameterType.GetGenericArguments()[0];
            }
        }

        if (eventType == null)
        {
            throw new ApplicationException(Resources.PipelineDelegateTypeException);
        }

        _delegates.TryAdd(eventType, []);
        _delegates[eventType].Add(new(handler, handler.Method.GetParameters().Select(item => item.ParameterType), position));

        return this;
    }

    public void Abort()
    {
        Aborted = true;
    }

    public void MarkExceptionHandled()
    {
        ExceptionHandled = true;
    }

    public IPipelineStage AddStage(string name)
    {
        var stage = new PipelineStage(Guard.AgainstEmpty(name));

        Stages.Add(stage);

        return stage;
    }

    public IPipelineStage GetStage(string name)
    {
        Guard.AgainstEmpty(name);

        var result = Stages.Find(stage => stage.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        Guard.Against<IndexOutOfRangeException>(result == null, string.Format(Resources.PipelineStageNotFound, name));

        return result!;
    }

    public virtual async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Aborted = false;
        Exception = null;

        LogEvent("Starting");
        await _pipelineOptions.PipelineStarting.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

        ReorderObservers();

        foreach (var stage in Stages)
        {
            StageName = stage.Name;

            LogEvent("Starting");
            await _pipelineOptions.StageStarting.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

            foreach (var eventType in stage.Events)
            {
                EventType = eventType;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await RaiseEventAsync(eventType, cancellationToken, false).ConfigureAwait(false);

                    if (!Aborted)
                    {
                        continue;
                    }

                    LogEvent("Aborted");
                    await RaiseEventAsync(_abortPipelineType, cancellationToken, true).ConfigureAwait(false);

                    return false;
                }
                catch (RecursiveException rex)
                {
                    Exception = rex;

                    Abort();

                    try
                    {
                        LogEvent("Aborted");
                        await RaiseEventAsync(_abortPipelineType, cancellationToken, true).ConfigureAwait(false);
                        LogEvent("RecursiveException");
                        await _pipelineOptions.PipelineRecursiveException.InvokeAsync(_pipelineEventArgs, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // give up
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Exception = ex.TrimLeading<TargetInvocationException>();

                    ExceptionHandled = false;

                    LogEvent("Failed");
                    await RaiseEventAsync(_pipelineFailedType, cancellationToken, true).ConfigureAwait(false);
                    await _pipelineOptions.PipelineFailed.InvokeAsync(_pipelineEventArgs, cancellationToken);

                    if (!ExceptionHandled)
                    {
                        throw;
                    }

                    if (!Aborted)
                    {
                        continue;
                    }

                    LogEvent("Aborted");
                    await RaiseEventAsync(_abortPipelineType, cancellationToken, true).ConfigureAwait(false);
                    await _pipelineOptions.PipelineAborted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

                    return false;
                }
            }

            EventType = null;

            LogEvent("Completed");
            await _pipelineOptions.StageCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);
        }

        StageName = string.Empty;

        LogEvent("Completed");
        await _pipelineOptions.PipelineCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private Pipeline AddObserver(IPipelineObserverProvider pipelineObserverProvider, ObserverPosition position)
    {
        var observerType = pipelineObserverProvider.GetObserverType();
        var observer = pipelineObserverProvider.GetObserverInstance();
        var implementationType = observer.GetType();

        var interfaceTypes = observerType.IsInterface
            ? new[] { observerType }.Concat(observerType.GetInterfaces())
            : implementationType.GetInterfaces();

        foreach (var eventInterface in interfaceTypes
                     .Where(item => item.IsGenericType && item.GetGenericTypeDefinition().IsAssignableFrom(PipelineObserverType)))
        {
            var pipelineEventType = eventInterface.GetGenericArguments()[0];

            if (!_observerMethodInvokers.TryGetValue(pipelineEventType, out _))
            {
                _observerMethodInvokers.Add(pipelineEventType, []);
            }

            var genericType = PipelineObserverType.MakeGenericType(pipelineEventType);
            var methodInfo = implementationType.GetInterfaceMap(genericType).TargetMethods.SingleOrDefault();

            if (methodInfo == null)
            {
                throw new PipelineException(string.Format(Resources.ObserverMethodNotFoundException, observerType.FullName, eventInterface.FullName));
            }

            _observerMethodInvokers[pipelineEventType].Add(new(pipelineObserverProvider, methodInfo, position));
        }

        return this;
    }

    private void LogEvent(string eventState)
    {
        LogMessage.PipelineEvent(_logger, $"{EventType?.FullName ?? "(no event)"}{(string.IsNullOrWhiteSpace(eventState) ? string.Empty : $"/{eventState}")}", GetType().FullName ?? "(no name)", !string.IsNullOrWhiteSpace(StageName) ? StageName : "(no stage)");
    }

    private async Task RaiseEventAsync(Type eventType, CancellationToken cancellationToken, bool ignoreAbort)
    {
        _observerMethodInvokers.TryGetValue(eventType, out var observersForEvent);
        _delegates.TryGetValue(eventType, out var delegatesForEvent);

        var hasObservers = observersForEvent is { Count: > 0 };
        var hasDelegates = delegatesForEvent is { Count: > 0 };

        if (!hasObservers && !hasDelegates)
        {
            return;
        }

        try
        {
            LogEvent("Starting");
            await _pipelineOptions.EventStarting.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

            PipelineContextConstructorInvoker? pipelineContextConstructor;

            await _lock.WaitAsync(cancellationToken);

            try
            {
                if (!_pipelineContextConstructors.TryGetValue(eventType, out pipelineContextConstructor))
                {
                    pipelineContextConstructor = new(this, eventType);

                    _pipelineContextConstructors.Add(eventType, pipelineContextConstructor);
                }
            }
            finally
            {
                _lock.Release();
            }

            var pipelineContext = pipelineContextConstructor.Create();

            if (hasObservers)
            {
                foreach (var observer in observersForEvent!)
                {
                    try
                    {
                        await observer.InvokeAsync(pipelineContext, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (eventType == _pipelineFailedType)
                        {
                            if (_pipelineOptions.PipelineRecursiveException.Count == 0)
                            {
                                throw new RecursiveException(Resources.ExceptionHandlerRecursiveException, ex);
                            }
                        }
                        else
                        {
                            throw new PipelineException(string.Format(_raisingPipelineEvent, eventType.FullName, StageName, observer.PipelineObserverProvider.GetType().FullName), ex);
                        }
                    }

                    if (Aborted && !ignoreAbort)
                    {
                        return;
                    }
                }
            }

            if (hasDelegates)
            {
                foreach (var observerDelegate in delegatesForEvent!)
                {
                    try
                    {
                        if (observerDelegate.HasParameters)
                        {
                            await (Task)observerDelegate.Handler.DynamicInvoke(observerDelegate.GetParameters(ServiceProvider, pipelineContext, cancellationToken))!;
                        }
                        else
                        {
                            await (Task)observerDelegate.Handler.DynamicInvoke()!;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (eventType == _pipelineFailedType)
                        {
                            if (_pipelineOptions.PipelineRecursiveException.Count == 0)
                            {
                                throw new RecursiveException(Resources.ExceptionHandlerRecursiveException, ex);
                            }
                        }
                        else
                        {
                            throw new PipelineException(string.Format(_raisingPipelineEvent, eventType.FullName, StageName, observerDelegate.GetType().FullName), ex);
                        }
                    }

                    if (Aborted && !ignoreAbort)
                    {
                        return;
                    }
                }
            }
        }
        finally
        {
            LogEvent("Completed");
            await _pipelineOptions.EventCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);
        }
    }

    private void ReorderObservers()
    {
        foreach (var key in _observerMethodInvokers.Keys.ToList())
        {
            var list = _observerMethodInvokers[key];

            if (list.Count <= 1)
            {
                continue;
            }

            _observerMethodInvokers[key] = list
                .OrderBy(item => (int)item.Position)
                .ToList();
        }

        foreach (var key in _delegates.Keys.ToList())
        {
            var list = _delegates[key];

            if (list.Count <= 1)
            {
                continue;
            }

            _delegates[key] = list
                .OrderBy(item => (int)item.Position)
                .ToList();
        }
    }

}
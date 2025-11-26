using System.Reflection;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;

namespace Shuttle.Core.Pipelines;

public class Pipeline : IPipeline
{
    private readonly PipelineOptions _pipelineOptions;
    private static readonly Type PipelineObserverType = typeof(IPipelineObserver<>);
    private static readonly Type PipelineContextType = typeof(IPipelineContext<>);

    private readonly Dictionary<Type, List<ObserverDelegate>> _delegates = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<Type, List<PipelineObserverMethodInvoker>> _observerMethodInvokers = new();

    private readonly Type _onAbortPipelineType = typeof(OnAbortPipeline);
    private readonly Type _onExecutionCancelledType = typeof(OnExecutionCancelled);
    private readonly Type _onPipelineExceptionType = typeof(OnPipelineException);
    private readonly Type _onStageCompletedType = typeof(OnStageCompleted);
    private readonly Type _onStageStartingType = typeof(OnStageStarting);
    private readonly Dictionary<Type, PipelineContextConstructorInvoker> _pipelineContextConstructors = new();

    private readonly PipelineEventArgs _pipelineEventArgs;

    private readonly string _raisingPipelineEvent = Resources.VerboseRaisingPipelineEvent;
    private readonly IServiceProvider _serviceProvider;

    private bool _initialized;

    protected List<IPipelineStage> Stages = [];

    public Pipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider)
    {
        _pipelineOptions = Guard.AgainstNull(Guard.AgainstNull(pipelineOptions).Value);
        _serviceProvider = Guard.AgainstNull(serviceProvider);

        Id = Guid.NewGuid();
        State = new State();

        _pipelineEventArgs = new(this);

        var stage = new PipelineStage("__PipelineEntry");

        stage.WithEvent<OnPipelineStarting>();

        Stages.Add(stage);
    }

    public Guid Id { get; }
    public bool ExceptionHandled { get; internal set; }
    public Exception? Exception { get; internal set; }
    public bool Aborted { get; internal set; }
    public string StageName { get; private set; } = "__PipelineEntry";
    public CancellationToken CancellationToken { get; private set; } = CancellationToken.None;

    public IState State { get; }

    public IPipeline AddObserver(IPipelineObserver pipelineObserver)
    {
        return AddObserver(new InstancePipelineObserverProvider(pipelineObserver));
    }

    public IPipeline AddObserver(Type observerType)
    {
        return AddObserver(new ServiceProviderPipelineObserverProvider(_serviceProvider, Guard.AgainstNull(observerType)));
    }

    public IPipeline AddObserver(Delegate handler)
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

        _delegates.TryAdd(eventType, new());
        _delegates[eventType].Add(new(handler, handler.Method.GetParameters().Select(item => item.ParameterType)));

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
        if (!_initialized)
        {
            Initialize();

            _initialized = true;
        }

        Aborted = false;
        Exception = null;

        CancellationToken = cancellationToken;

        await _pipelineOptions.PipelineStarting.InvokeAsync(_pipelineEventArgs, cancellationToken);

        foreach (var stage in Stages)
        {
            StageName = stage.Name;

            await _pipelineOptions.StageStarting.InvokeAsync(_pipelineEventArgs, cancellationToken);

            foreach (var eventType in stage.Events)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        await RaiseEventAsync(_onExecutionCancelledType, false).ConfigureAwait(false);

                        return false;
                    }

                    await RaiseEventAsync(eventType, false).ConfigureAwait(false);

                    if (!Aborted)
                    {
                        continue;
                    }

                    await RaiseEventAsync(_onAbortPipelineType, true).ConfigureAwait(false);

                    return false;
                }
                catch (RecursiveException)
                {
                    Abort();

                    try
                    {
                        await RaiseEventAsync(_onAbortPipelineType, true).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // give up
                    }
                }
                catch (Exception ex)
                {
                    Exception = ex.TrimLeading<TargetInvocationException>();

                    ExceptionHandled = false;

                    await RaiseEventAsync(_onPipelineExceptionType, true).ConfigureAwait(false);

                    if (!ExceptionHandled)
                    {
                        throw;
                    }

                    if (!Aborted)
                    {
                        continue;
                    }

                    await RaiseEventAsync(_onAbortPipelineType, true).ConfigureAwait(false);

                    return false;
                }
            }

            await _pipelineOptions.StageCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken);
        }

        await _pipelineOptions.PipelineCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken);

        return true;
    }

    private bool HandlesType(Type type)
    {
        return _observerMethodInvokers.ContainsKey(type) || _delegates.ContainsKey(type);
    }

    private void Initialize()
    {
        var optimizedStages = new List<IPipelineStage>();

        foreach (var stage in Stages)
        {
            var events = new List<Type>();

            if (HandlesType(_onStageStartingType))
            {
                events.Add(_onStageStartingType);
            }

            events.AddRange(stage.Events.Where(HandlesType));

            if (HandlesType(_onStageCompletedType))
            {
                events.Add(_onStageCompletedType);
            }

            if (events.Any())
            {
                var optimizedStage = new PipelineStage(stage.Name);

                foreach (var @event in events)
                {
                    optimizedStage.WithEvent(@event);
                }

                optimizedStages.Add(optimizedStage);
            }
        }

        Stages = optimizedStages;
    }

    private async Task RaiseEventAsync(Type eventType, bool ignoreAbort)
    {
        _observerMethodInvokers.TryGetValue(eventType, out var observersForEvent);
        _delegates.TryGetValue(eventType, out var delegatesForEvent);

        var hasObservers = observersForEvent is { Count: > 0 };
        var hasDelegates = delegatesForEvent is { Count: > 0 };

        if (!hasObservers && !hasDelegates)
        {
            return;
        }

        PipelineContextConstructorInvoker? pipelineContextConstructor;

        await _lock.WaitAsync(CancellationToken);

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
                    await observer.InvokeAsync(pipelineContext).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (eventType == _onPipelineExceptionType)
                    {
                        if (_pipelineOptions.PipelineRecursiveException.Count == 0)
                        {
                            throw new RecursiveException(Resources.ExceptionHandlerRecursiveException, ex);
                        }

                        await _pipelineOptions.PipelineRecursiveException.InvokeAsync(_pipelineEventArgs, CancellationToken);
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
                        await (Task)observerDelegate.Handler.DynamicInvoke(observerDelegate.GetParameters(_serviceProvider, pipelineContext))!;
                    }
                    else
                    {
                        await (Task)observerDelegate.Handler.DynamicInvoke()!;
                    }
                }
                catch (Exception ex)
                {
                    if (eventType == _onPipelineExceptionType)
                    {
                        if (_pipelineOptions.PipelineRecursiveException.Count == 0)
                        {
                            throw new RecursiveException(Resources.ExceptionHandlerRecursiveException, ex);
                        }

                        await _pipelineOptions.PipelineRecursiveException.InvokeAsync(_pipelineEventArgs, CancellationToken);
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

    private IPipeline AddObserver(IPipelineObserverProvider pipelineObserverProvider)
    {
        var observerType = pipelineObserverProvider.GetObserverType();

        foreach (var eventInterface in observerType.GetInterfaces()
                     .Where(item => item.IsGenericType && item.GetGenericTypeDefinition().IsAssignableFrom(PipelineObserverType)))
        {
            var pipelineEventType = eventInterface.GetGenericArguments()[0];

            if (!_observerMethodInvokers.TryGetValue(pipelineEventType, out _))
            {
                _observerMethodInvokers.Add(pipelineEventType, new());
            }

            var genericType = PipelineObserverType.MakeGenericType(pipelineEventType);

            var methodInfo = observerType.GetInterfaceMap(genericType).TargetMethods.SingleOrDefault();

            if (methodInfo == null)
            {
                throw new PipelineException(string.Format(Resources.ObserverMethodNotFoundException, observerType.FullName, eventInterface.FullName));
            }

            _observerMethodInvokers[pipelineEventType].Add(new(pipelineObserverProvider, methodInfo));
        }

        return this;
    }
}
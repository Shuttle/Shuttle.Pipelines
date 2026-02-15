using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Core.Pipelines;

public class Pipeline : IPipeline
{
    private static readonly Type PipelineObserverType = typeof(IPipelineObserver<>);
    private static readonly Type PipelineContextType = typeof(IPipelineContext<>);

    private readonly Type _abortPipelineType = typeof(AbortPipeline);
    private readonly Type _completeTransactionScopeType = typeof(CompleteTransactionScope);
    private readonly Dictionary<Type, List<ObserverDelegate>> _delegates = new();
    private readonly Type _disposeTransactionScopeType = typeof(DisposeTransactionScope);

    private readonly ILogger<Pipeline> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<Type, List<PipelineObserverMethodInvoker>> _observerMethodInvokers = new();
    private readonly Dictionary<Type, PipelineContextConstructorInvoker> _pipelineContextConstructors = new();

    private readonly PipelineEventArgs _pipelineEventArgs;
    private readonly Type _pipelineFailedType = typeof(PipelineFailed);
    private readonly PipelineOptions _pipelineOptions;

    private readonly string _raisingPipelineEvent = Resources.VerboseRaisingPipelineEvent;
    private readonly Type _startTransactionScopeType = typeof(StartTransactionScope);
    private readonly ITransactionScopeFactory _transactionScopeFactory;
    private readonly TransactionScopeOptions _transactionScopeOptions;

    private ITransactionScope? _transactionScope;

    protected List<IPipelineStage> Stages = [];

    public Pipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, ILogger<Pipeline>? logger = null)
    {
        _logger = logger ?? NullLogger<Pipeline>.Instance;
        _pipelineOptions = Guard.AgainstNull(Guard.AgainstNull(pipelineOptions).Value);
        _transactionScopeOptions = Guard.AgainstNull(Guard.AgainstNull(transactionScopeOptions).Value);
        _transactionScopeFactory = Guard.AgainstNull(transactionScopeFactory);
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

    public IPipeline AddObserver(IPipelineObserver pipelineObserver)
    {
        return AddObserver(new InstancePipelineObserverProvider(pipelineObserver));
    }

    public IPipeline AddObserver(Type observerType)
    {
        return AddObserver(new ServiceProviderPipelineObserverProvider(ServiceProvider, Guard.AgainstNull(observerType)));
    }

    public IPipeline AddObserver<T>()
    {
        return AddObserver(typeof(T));
    }

    public IPipeline AddObserver<TDelegate>(TDelegate handler) where TDelegate : Delegate
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
        Aborted = false;
        Exception = null;

        await LogEventAsync("Starting", cancellationToken).ConfigureAwait(false);
        await _pipelineOptions.PipelineStarting.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

        foreach (var stage in Stages)
        {
            StageName = stage.Name;

            if (stage.RequiresTransactionScope)
            {
                if (_transactionScope != null)
                {
                    throw new PipelineException(Resources.TransactionScopeAlreadyStartedException);
                }

                if (Transaction.Current == null)
                {
                    EventType = _startTransactionScopeType;

                    var transactionScopeEventArgs = new TransactionScopeEventArgs(this, _transactionScopeOptions.IsolationLevel, _transactionScopeOptions.Timeout);

                    await LogEventAsync("Starting", cancellationToken).ConfigureAwait(false);
                    await _pipelineOptions.TransactionScopeStarting.InvokeAsync(transactionScopeEventArgs, cancellationToken);

                    // This cannot be async as the transaction scope will not be created within this execution context.
                    _transactionScope = _transactionScopeFactory.Create(transactionScopeEventArgs.IsolationLevel, transactionScopeEventArgs.Timeout);

                    State.SetTransactionScope(_transactionScope);
                }
                else
                {
                    await LogEventAsync("Ignored", cancellationToken).ConfigureAwait(false);
                    await _pipelineOptions.TransactionScopeIgnored.InvokeAsync(_pipelineEventArgs, cancellationToken);
                }
            }

            await LogEventAsync("Starting", cancellationToken).ConfigureAwait(false);
            await _pipelineOptions.StageStarting.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

            foreach (var eventType in stage.Events)
            {
                EventType = eventType;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (eventType == _completeTransactionScopeType && _transactionScope != null)
                    {
                        await _pipelineOptions.EventStarting.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

                        _transactionScope.Complete();
                        _transactionScope.Dispose();
                        _transactionScope = null;

                        State.Remove("TransactionScope");

                        await _pipelineOptions.EventCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

                        continue;
                    }

                    if (eventType == _disposeTransactionScopeType && _transactionScope != null)
                    {
                        await _pipelineOptions.EventStarting.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);
                        DisposeTransactionScope();
                        await _pipelineOptions.EventCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

                        continue;
                    }

                    await RaiseEventAsync(eventType, cancellationToken, false).ConfigureAwait(false);

                    if (!Aborted)
                    {
                        continue;
                    }

                    DisposeTransactionScope();

                    await LogEventAsync("Aborted", cancellationToken).ConfigureAwait(false);
                    await RaiseEventAsync(_abortPipelineType, cancellationToken, true).ConfigureAwait(false);

                    return false;
                }
                catch (OperationCanceledException)
                {
                    DisposeTransactionScope();

                    throw;
                }
                catch (RecursiveException rex)
                {
                    Exception = rex;

                    Abort();

                    try
                    {
                        await LogEventAsync("Aborted", cancellationToken).ConfigureAwait(false);
                        await RaiseEventAsync(_abortPipelineType, cancellationToken, true).ConfigureAwait(false);
                        await LogEventAsync("RecursiveException", cancellationToken).ConfigureAwait(false);
                        await _pipelineOptions.PipelineRecursiveException.InvokeAsync(_pipelineEventArgs, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // give up
                    }
                }
                catch (Exception ex)
                {
                    DisposeTransactionScope();

                    Exception = ex.TrimLeading<TargetInvocationException>();

                    ExceptionHandled = false;

                    await LogEventAsync("Failed", cancellationToken).ConfigureAwait(false);
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

                    await LogEventAsync("Aborted", cancellationToken).ConfigureAwait(false);
                    await RaiseEventAsync(_abortPipelineType, cancellationToken, true).ConfigureAwait(false);
                    await _pipelineOptions.PipelineAborted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

                    return false;
                }
            }

            EventType = null;

            await LogEventAsync("Completed", cancellationToken).ConfigureAwait(false);
            await _pipelineOptions.StageCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

            DisposeTransactionScope();
        }

        StageName = string.Empty;

        await LogEventAsync("Completed", cancellationToken).ConfigureAwait(false);
        await _pipelineOptions.PipelineCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private Pipeline AddObserver(IPipelineObserverProvider pipelineObserverProvider)
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
                _observerMethodInvokers.Add(pipelineEventType, new());
            }

            var genericType = PipelineObserverType.MakeGenericType(pipelineEventType);
            var methodInfo = implementationType.GetInterfaceMap(genericType).TargetMethods.SingleOrDefault();

            if (methodInfo == null)
            {
                throw new PipelineException(string.Format(Resources.ObserverMethodNotFoundException, observerType.FullName, eventInterface.FullName));
            }

            _observerMethodInvokers[pipelineEventType].Add(new(pipelineObserverProvider, methodInfo));
        }

        return this;
    }

    private void DisposeTransactionScope()
    {
        _transactionScope?.Dispose();
        _transactionScope = null;

        State.Remove("TransactionScope");
    }

    private async Task LogEventAsync(string eventState, CancellationToken cancellationToken)
    {
        LogMessage.PipelineEvent(_logger, $"{EventType?.FullName ?? "(no event)"}{(string.IsNullOrWhiteSpace(eventState) ? string.Empty : $"/{eventState}")}", GetType().FullName ?? "(no name)", !string.IsNullOrWhiteSpace(StageName) ? StageName : "(no stage)");

        await Task.CompletedTask;
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
            await LogEventAsync("Starting", cancellationToken).ConfigureAwait(false);
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
            await LogEventAsync("Completed", cancellationToken).ConfigureAwait(false);
            await _pipelineOptions.EventCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);
        }
    }
}
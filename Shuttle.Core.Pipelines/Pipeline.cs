using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;
using Shuttle.Core.TransactionScope;
using System.Reflection;

namespace Shuttle.Core.Pipelines;

public class Pipeline : IPipeline
{
    private readonly IPipelineDependencies _pipelineDependencies;

    private static readonly Type PipelineObserverType = typeof(IPipelineObserver<>);
    private static readonly Type PipelineContextType = typeof(IPipelineContext<>);

    private readonly Dictionary<Type, List<ObserverDelegate>> _delegates = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<Type, List<PipelineObserverMethodInvoker>> _observerMethodInvokers = new();

    private readonly Type _abortPipelineType = typeof(AbortPipeline);
    private readonly Type _completeTransactionScopeType = typeof(CompleteTransactionScope);
    private readonly Type _disposeTransactionScopeType = typeof(DisposeTransactionScope);
    private readonly Type _executionCancelledType = typeof(ExecutionCancelled);
    private readonly Type _pipelineExceptionType = typeof(PipelineFailed);
    private readonly Type _stageCompletedType = typeof(StageCompleted);
    private readonly Type _stageStartingType = typeof(StageStarting);
    private readonly Dictionary<Type, PipelineContextConstructorInvoker> _pipelineContextConstructors = new();

    private readonly PipelineEventArgs _pipelineEventArgs;

    private readonly string _raisingPipelineEvent = Resources.VerboseRaisingPipelineEvent;

    private bool _initialized;
    private ITransactionScope? _transactionScope;

    protected List<IPipelineStage> Stages = [];

    public Pipeline(IPipelineDependencies pipelineDependencies)
    {
        _pipelineDependencies = Guard.AgainstNull(pipelineDependencies);

        Id = Guid.NewGuid();
        State = new State();

        _pipelineEventArgs = new(this);

        var stage = new PipelineStage("__PipelineEntry");

        stage.WithEvent<PipelineStarting>();

        Stages.Add(stage);
    }

    public Guid Id { get; }
    public bool ExceptionHandled { get; internal set; }
    public Exception? Exception { get; internal set; }
    public bool Aborted { get; internal set; }
    public string StageName { get; private set; } = "__PipelineEntry";

    public IState State { get; }

    public IPipeline AddObserver(IPipelineObserver pipelineObserver)
    {
        return AddObserver(new InstancePipelineObserverProvider(pipelineObserver));
    }

    public IPipeline AddObserver(Type observerType)
    {
        return AddObserver(new ServiceProviderPipelineObserverProvider(_pipelineDependencies.ServiceProvider, Guard.AgainstNull(observerType)));
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

        await _pipelineDependencies.PipelineOptions.PipelineStarting.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

        foreach (var stage in Stages)
        {
            StageName = stage.Name;

            await StartTransactionScopeAsync(cancellationToken);

            await _pipelineDependencies.PipelineOptions.StageStarting.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);
            
            foreach (var eventType in stage.Events)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (eventType == _completeTransactionScopeType)
                    {
                        _transactionScope?.Complete();
                        _transactionScope = null;
                        State.Remove("TransactionScope");

                        continue;
                    }

                    if (eventType == _disposeTransactionScopeType)
                    {
                        await DisposeTransactionScopeAsync();

                        continue;
                    }

                    await RaiseEventAsync(eventType, cancellationToken, false).ConfigureAwait(false);

                    if (!Aborted)
                    {
                        continue;
                    }

                    await RaiseEventAsync(_abortPipelineType, cancellationToken, true).ConfigureAwait(false);

                    return false;
                }
                catch (OperationCanceledException)
                {
                    await DisposeTransactionScopeAsync();

                    try
                    {
                        await RaiseEventAsync(_executionCancelledType, cancellationToken, false).ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignore
                    }

                    throw;
                }
                catch (RecursiveException)
                {
                    Abort();

                    try
                    {
                        await RaiseEventAsync(_abortPipelineType, cancellationToken, true).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // give up
                    }
                }
                catch (Exception ex)
                {
                    await DisposeTransactionScopeAsync();

                    Exception = ex.TrimLeading<TargetInvocationException>();

                    ExceptionHandled = false;

                    await RaiseEventAsync(_pipelineExceptionType, cancellationToken, true).ConfigureAwait(false);

                    if (!ExceptionHandled)
                    {
                        throw;
                    }

                    if (!Aborted)
                    {
                        continue;
                    }

                    await RaiseEventAsync(_abortPipelineType, cancellationToken, true).ConfigureAwait(false);

                    return false;
                }
            }

            await _pipelineDependencies.PipelineOptions.StageCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

            await DisposeTransactionScopeAsync();
        }

        await _pipelineDependencies.PipelineOptions.PipelineCompleted.InvokeAsync(_pipelineEventArgs, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private async Task DisposeTransactionScopeAsync()
    {
        await (_transactionScope?.TryDisposeAsync() ?? Task.CompletedTask);
        _transactionScope = null;
        State.Remove("TransactionScope");
    }

    private async Task StartTransactionScopeAsync(CancellationToken cancellationToken)
    {
        if (_pipelineDependencies.PipelineOptions.RequiresTransactionScope(GetType(), StageName))
        {
            if (_transactionScope != null)
            {
                throw new PipelineException(Resources.TransactionScopeAlreadyStartedException);
            }

            var transactionScopeEventArgs = new TransactionScopeEventArgs(this, StageName, _pipelineDependencies.TransactionScopeOptions.IsolationLevel, _pipelineDependencies.TransactionScopeOptions.Timeout);

            await _pipelineDependencies.PipelineOptions.TransactionScopeStarting.InvokeAsync(transactionScopeEventArgs, cancellationToken);

            _transactionScope = _pipelineDependencies.TransactionScopeFactory.Create(transactionScopeEventArgs.IsolationLevel, transactionScopeEventArgs.Timeout);

            State.SetTransactionScope(_transactionScope);
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

            if (HandlesType(_stageStartingType))
            {
                events.Add(_stageStartingType);
            }

            events.AddRange(stage.Events.Where(HandlesType));

            if (HandlesType(_stageCompletedType))
            {
                events.Add(_stageCompletedType);
            }

            if (!events.Any())
            {
                continue;
            }

            var optimizedStage = new PipelineStage(stage.Name);

            foreach (var @event in events)
            {
                optimizedStage.WithEvent(@event);
            }

            optimizedStages.Add(optimizedStage);
        }

        Stages = optimizedStages;
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
                catch (Exception ex)
                {
                    if (eventType == _pipelineExceptionType)
                    {
                        if (_pipelineDependencies.PipelineOptions.PipelineRecursiveException.Count == 0)
                        {
                            throw new RecursiveException(Resources.ExceptionHandlerRecursiveException, ex);
                        }

                        await _pipelineDependencies.PipelineOptions.PipelineRecursiveException.InvokeAsync(_pipelineEventArgs, cancellationToken);
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
                        await (Task)observerDelegate.Handler.DynamicInvoke(observerDelegate.GetParameters(_pipelineDependencies.ServiceProvider, pipelineContext, cancellationToken))!;
                    }
                    else
                    {
                        await (Task)observerDelegate.Handler.DynamicInvoke()!;
                    }
                }
                catch (Exception ex)
                {
                    if (eventType == _pipelineExceptionType)
                    {
                        if (_pipelineDependencies.PipelineOptions.PipelineRecursiveException.Count == 0)
                        {
                            throw new RecursiveException(Resources.ExceptionHandlerRecursiveException, ex);
                        }

                        await _pipelineDependencies.PipelineOptions.PipelineRecursiveException.InvokeAsync(_pipelineEventArgs, cancellationToken);
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
}
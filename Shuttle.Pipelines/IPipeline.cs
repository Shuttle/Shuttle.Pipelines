namespace Shuttle.Pipelines;

public interface IPipeline
{
    bool Aborted { get; }
    Type? EventType { get; }
    Exception? Exception { get; }
    bool ExceptionHandled { get; }

    Guid Id { get; }
    IServiceProvider ServiceProvider { get; }
    string StageName { get; }
    IState State { get; }
    void Abort();

    IPipeline AddObserver(IPipelineObserver pipelineObserver, ObserverPosition position = ObserverPosition.Anywhere);
    IPipeline AddObserver(Type observerType, ObserverPosition position = ObserverPosition.Anywhere);
    IPipeline AddObserver<TDelegate>(TDelegate handler, ObserverPosition position = ObserverPosition.Anywhere) where TDelegate : Delegate;
    IPipeline AddObserver<T>(ObserverPosition position = ObserverPosition.Anywhere);
    IPipelineStage AddStage(string name);
    Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);
    IPipelineStage GetStage(string name);
    void MarkExceptionHandled();
}
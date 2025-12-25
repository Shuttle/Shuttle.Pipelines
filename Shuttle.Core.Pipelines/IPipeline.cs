namespace Shuttle.Core.Pipelines;

public interface IPipeline
{
    bool Aborted { get; }
    Exception? Exception { get; }
    bool ExceptionHandled { get; }

    Guid Id { get; }
    string StageName { get; }
    Type? EventType { get; }
    IState State { get; }
    void Abort();

    IPipeline AddObserver(IPipelineObserver pipelineObserver);
    IPipeline AddObserver(Type observerType);
    IPipeline AddObserver<TDelegate>(TDelegate handler) where TDelegate : Delegate;
    IPipelineStage AddStage(string name);
    Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);
    IPipelineStage GetStage(string name);
    void MarkExceptionHandled();
}
namespace Shuttle.Core.Pipelines;

public interface IPipeline
{
    bool Aborted { get; }
    Exception? Exception { get; }
    bool ExceptionHandled { get; }

    Guid Id { get; }
    string StageName { get; }
    IState State { get; }
    void Abort();

    IPipeline AddObserver(IPipelineObserver pipelineObserver);
    IPipeline AddObserver(Type observerType);
    IPipeline AddObserver(Delegate handler);
    IPipelineStage AddStage(string name);
    Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);
    IPipelineStage GetStage(string name);
    void MarkExceptionHandled();
}
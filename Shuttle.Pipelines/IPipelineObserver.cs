namespace Shuttle.Pipelines;

public interface IPipelineObserver
{
}

public interface IPipelineObserver<TPipelineEvent> : IPipelineObserver where TPipelineEvent : class
{
    Task ExecuteAsync(IPipelineContext<TPipelineEvent> pipelineContext, CancellationToken cancellationToken = default);
}
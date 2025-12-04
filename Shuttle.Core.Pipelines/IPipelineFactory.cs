namespace Shuttle.Core.Pipelines;

public interface IPipelineFactory
{
    void Flush();
    Task<TPipeline> GetPipelineAsync<TPipeline>(CancellationToken cancellationToken = default) where TPipeline : IPipeline;
    Task ReleasePipelineAsync(IPipeline pipeline, CancellationToken cancellationToken = default);
}
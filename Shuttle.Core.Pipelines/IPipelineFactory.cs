namespace Shuttle.Core.Pipelines;

public interface IPipelineFactory
{
    Task<TPipeline> GetPipelineAsync<TPipeline>(CancellationToken cancellationToken = default) where TPipeline : IPipeline;
}
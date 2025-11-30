using System;

namespace Shuttle.Core.Pipelines;

public interface IPipelineFactory
{
    Task<TPipeline> GetPipelineAsync<TPipeline>(CancellationToken cancellationToken = default) where TPipeline : IPipeline;
    Task ReleasePipelineAsync(IPipeline pipeline, CancellationToken cancellationToken = default);
    void Flush();
}
using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class PipelineContext<T>(IPipeline pipeline) : IPipelineContext<T>
{
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
}
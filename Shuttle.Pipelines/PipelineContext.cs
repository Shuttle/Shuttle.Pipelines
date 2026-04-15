using Shuttle.Contract;

namespace Shuttle.Pipelines;

public class PipelineContext<T>(IPipeline pipeline) : IPipelineContext<T>
{
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
}
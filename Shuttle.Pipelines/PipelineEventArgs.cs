using Shuttle.Contract;

namespace Shuttle.Pipelines;

public class PipelineEventArgs(IPipeline pipeline)
{
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
}
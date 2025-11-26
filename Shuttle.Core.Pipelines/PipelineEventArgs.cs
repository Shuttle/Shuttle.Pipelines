using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class PipelineEventArgs(IPipeline pipeline)
{
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
}
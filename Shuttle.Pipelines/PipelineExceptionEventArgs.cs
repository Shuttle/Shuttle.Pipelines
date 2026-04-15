using Shuttle.Contract;

namespace Shuttle.Pipelines;

public class PipelineExceptionEventArgs(IPipeline pipeline)
{
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
}
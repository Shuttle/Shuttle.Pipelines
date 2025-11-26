using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class PipelineExceptionEventArgs(IPipeline pipeline)
{
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
}
using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class PipelineOptimizationEventArgs(IPipeline pipeline, string optimization)
{
    public string Optimization { get; } = Guard.AgainstEmpty(optimization);
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
}
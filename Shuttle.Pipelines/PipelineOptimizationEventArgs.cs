using Shuttle.Contract;

namespace Shuttle.Pipelines;

public class PipelineOptimizationEventArgs(IPipeline pipeline, string optimization)
{
    public string Optimization { get; } = Guard.AgainstEmpty(optimization);
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
}
using Shuttle.Contract;

namespace Shuttle.Pipelines;

public class PipelineContextEventArgs(IPipelineContext pipelineContext)
{
    public IPipelineContext PipelineContext { get; } = Guard.AgainstNull(pipelineContext);
}
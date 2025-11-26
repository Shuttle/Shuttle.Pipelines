using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class PipelineContextEventArgs(IPipelineContext pipelineContext)
{
    public IPipelineContext PipelineContext { get; } = Guard.AgainstNull(pipelineContext);
}
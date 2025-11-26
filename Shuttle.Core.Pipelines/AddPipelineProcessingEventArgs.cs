using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class AddPipelineProcessingEventArgs(PipelineProcessingBuilder pipelineProcessingBuilder)
{
    public PipelineProcessingBuilder PipelineProcessingBuilder { get; } = Guard.AgainstNull(pipelineProcessingBuilder);
}
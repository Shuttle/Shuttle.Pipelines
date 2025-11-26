using Shuttle.Extensions.Options;

namespace Shuttle.Core.Pipelines;

public class PipelineOptions
{
    public bool ReusePipelines { get; set; } = true;
    public AsyncEvent<PipelineEventArgs> PipelineCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineCreated { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineObtained { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineRecursiveException { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineReleased { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineStarting { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> StageCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> StageStarting { get; set; } = new();
}
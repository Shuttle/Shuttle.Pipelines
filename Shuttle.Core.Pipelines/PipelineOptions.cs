using Shuttle.Extensions.Options;

namespace Shuttle.Core.Pipelines;

public class PipelineOptions
{
    public AsyncEvent<PipelineEventArgs> PipelineAborted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineFailed { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineRecursiveException { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineStarting { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> StageCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> StageStarting { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> EventCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> EventStarting { get; set; } = new();
}
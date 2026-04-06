using Microsoft.Extensions.Options;

namespace Shuttle.Core.Pipelines.Tests;

public class AmbientDataPipeline : Pipeline
{
    public AmbientDataPipeline(IOptions<PipelineOptions> pipelineOptions, IPipelineState pipelineState, IServiceProvider serviceProvider, IAmbientDataService ambientDataService)
        : base(pipelineOptions, pipelineState, serviceProvider)
    {
        AddStage("Pipeline")
            .WithEvent<OnAddValue>()
            .WithEvent<OnGetValue>()
            .WithEvent<OnRemoveValue>();

        AddObserver(new AmbientDataObserver(ambientDataService));
    }
}
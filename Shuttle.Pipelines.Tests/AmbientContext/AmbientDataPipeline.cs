using Microsoft.Extensions.Options;

namespace Shuttle.Pipelines.Tests;

public class AmbientDataPipeline : Pipeline
{
    public AmbientDataPipeline(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider, IAmbientDataService ambientDataService)
        : base(pipelineOptions, serviceProvider)
    {
        AddStage("Pipeline")
            .WithEvent<OnAddValue>()
            .WithEvent<OnGetValue>()
            .WithEvent<OnRemoveValue>();

        AddObserver(new AmbientDataObserver(ambientDataService));
    }
}
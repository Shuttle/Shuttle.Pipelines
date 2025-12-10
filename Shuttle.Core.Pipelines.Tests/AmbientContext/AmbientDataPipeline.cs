namespace Shuttle.Core.Pipelines.Tests;

public class AmbientDataPipeline : Pipeline
{
    public AmbientDataPipeline(IPipelineDependencies pipelineDependencies, IAmbientDataService ambientDataService)
        : base(pipelineDependencies)
    {
        AddStage("Pipeline")
            .WithEvent<OnAddValue>()
            .WithEvent<OnGetValue>()
            .WithEvent<OnRemoveValue>();

        AddObserver(new AmbientDataObserver(ambientDataService));
    }
}
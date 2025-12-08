namespace Shuttle.Core.Pipelines.Tests;

public class AmbientDataPipeline : Pipeline
{
    public AmbientDataPipeline(Context context, IAmbientDataService ambientDataService)
        : base(context)
    {
        AddStage("Pipeline")
            .WithEvent<OnAddValue>()
            .WithEvent<OnGetValue>()
            .WithEvent<OnRemoveValue>();

        AddObserver(new AmbientDataObserver(ambientDataService));
    }
}
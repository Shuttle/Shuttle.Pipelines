namespace Shuttle.Pipelines;

public interface IAddEventBefore
{
    IPipelineStage Add<TPipelineEvent>() where TPipelineEvent : class, new();
}
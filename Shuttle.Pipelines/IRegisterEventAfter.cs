namespace Shuttle.Pipelines;

public interface IAddEventAfter
{
    IPipelineStage Add<TPipelineEvent>() where TPipelineEvent : class, new();
}
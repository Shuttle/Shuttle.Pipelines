using Shuttle.Contract;

namespace Shuttle.Pipelines;

public class AddEventBefore(IPipelineStage pipelineStage, List<Type> eventsToExecute, Type pipelineEvent)
    : IAddEventBefore
{
    private readonly List<Type> _eventsToExecute = Guard.AgainstNull(eventsToExecute);
    private readonly Type _pipelineEvent = Guard.AgainstNull(pipelineEvent);
    private readonly IPipelineStage _pipelineStage = Guard.AgainstNull(pipelineStage);

    public IPipelineStage Add<TPipelineEvent>() where TPipelineEvent : class, new()
    {
        var index = _eventsToExecute.IndexOf(_pipelineEvent);

        _eventsToExecute.Insert(index, typeof(TPipelineEvent));

        return _pipelineStage;
    }
}
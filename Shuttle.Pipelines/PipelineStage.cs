using System.Collections.ObjectModel;
using Shuttle.Contract;

namespace Shuttle.Pipelines;

public class PipelineStage(string name) : IPipelineStage
{
    private readonly List<Type> _pipelineEvents = [];

    public string Name { get; } = Guard.AgainstNull(name);
    public IEnumerable<Type> Events => new ReadOnlyCollection<Type>(_pipelineEvents);

    public IPipelineStage WithEvent<TEvent>() where TEvent : class
    {
        return WithEvent(typeof(TEvent));
    }

    public IAddEventBefore BeforeEvent<TEvent>() where TEvent : class
    {
        var eventName = typeof(TEvent).FullName ?? throw new ApplicationException(string.Format(Reflection.Resources.TypeFullNameNullException, typeof(TEvent).Name));
        var pipelineEvent = _pipelineEvents.Find(e => (e.FullName ?? throw new ApplicationException(string.Format(Reflection.Resources.TypeFullNameNullException, typeof(TEvent).Name))).Equals(eventName));

        return pipelineEvent == null
            ? throw new InvalidOperationException(string.Format(Resources.PipelineStageEventNotRegistered, Name, eventName))
            : new AddEventBefore(this, _pipelineEvents, pipelineEvent);
    }

    public IAddEventAfter AfterEvent<TEvent>() where TEvent : class
    {
        var eventName = typeof(TEvent).FullName;
        var pipelineEvent = _pipelineEvents.Find(e => (e.FullName ?? throw new ApplicationException(string.Format(Reflection.Resources.TypeFullNameNullException, typeof(TEvent).Name))).Equals(eventName));

        return pipelineEvent == null
            ? throw new InvalidOperationException(string.Format(Resources.PipelineStageEventNotRegistered, Name, eventName))
            : new AddEventAfter(this, _pipelineEvents, pipelineEvent);
    }

    public IPipelineStage WithEvent(Type eventType)
    {
        Guard.AgainstNull(eventType);

        if (_pipelineEvents.Contains(eventType))
        {
            throw new InvalidOperationException(string.Format(Resources.PipelineStageEventAlreadyRegisteredException, Name, eventType.FullName));
        }

        _pipelineEvents.Add(eventType);

        return this;
    }
}
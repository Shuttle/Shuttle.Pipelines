namespace Shuttle.Core.Pipelines;

public interface IPipelineStage
{
    IEnumerable<Type> Events { get; }
    string Name { get; }
    IAddEventAfter AfterEvent<TEvent>() where TEvent : class;
    IAddEventBefore BeforeEvent<TEvent>() where TEvent : class;
    IPipelineStage WithEvent<TEvent>() where TEvent : class;
}
    
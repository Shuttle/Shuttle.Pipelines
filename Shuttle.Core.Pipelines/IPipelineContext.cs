namespace Shuttle.Core.Pipelines;

public interface IPipelineContext
{
    Type EventType { get; }
    IPipeline Pipeline { get; }
}

public interface IPipelineContext<T> : IPipelineContext
{
}
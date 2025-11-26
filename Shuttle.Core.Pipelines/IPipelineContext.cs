namespace Shuttle.Core.Pipelines;

public interface IPipelineContext
{
    IPipeline Pipeline { get; }
    Type EventType { get; }
}

public interface IPipelineContext<T> : IPipelineContext
{
}
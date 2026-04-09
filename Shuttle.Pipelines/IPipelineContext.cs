namespace Shuttle.Pipelines;

public interface IPipelineContext
{
    IPipeline Pipeline { get; }
}

public interface IPipelineContext<T> : IPipelineContext
{
}
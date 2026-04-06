namespace Shuttle.Core.Pipelines;

public interface IPipelineState
{
    IState State { get; }
}
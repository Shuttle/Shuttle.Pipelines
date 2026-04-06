namespace Shuttle.Core.Pipelines;

public class PipelineState : IPipelineState
{
    public IState State { get; } = new State();
}
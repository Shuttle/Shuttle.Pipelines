namespace Shuttle.Pipelines.Tests;

public class InterfacedObserver : IInterfacedObserver
{
    public bool Called { get; private set; }

    public async Task ExecuteAsync(IPipelineContext<MockPipelineEvent1> pipelineContext, CancellationToken cancellationToken = default)
    {
        Called = true;

        await Task.CompletedTask;
    }
}

public interface IInterfacedObserver : IPipelineObserver<MockPipelineEvent1>
{
}
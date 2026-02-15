namespace Shuttle.Core.Pipelines.Tests;

public interface IMockAuthenticateObserver :
    IPipelineObserver<MockPipelineEvent1>,
    IPipelineObserver<MockPipelineEvent2>,
    IPipelineObserver<MockPipelineEvent3>;

public class MockAuthenticateObserver : IMockAuthenticateObserver
{
    public string CallSequence { get; private set; } = string.Empty;

    public async Task ExecuteAsync(IPipelineContext<MockPipelineEvent1> pipelineContext, CancellationToken cancellationToken = default)
    {
        Execute(pipelineContext.Pipeline.EventType!, 1);

        await Task.CompletedTask;
    }

    public async Task ExecuteAsync(IPipelineContext<MockPipelineEvent2> pipelineContext, CancellationToken cancellationToken = default)
    {
        Execute(pipelineContext.Pipeline.EventType!, 2);

        await Task.CompletedTask;
    }

    public async Task ExecuteAsync(IPipelineContext<MockPipelineEvent3> pipelineContext, CancellationToken cancellationToken = default)
    {
        Execute(pipelineContext.Pipeline.EventType!, 3);

        await Task.CompletedTask;
    }

    private void Execute(Type eventType, int delta)
    {
        Console.WriteLine(@"[collection] : {0}", eventType.Name);

        CallSequence += delta.ToString();
    }
}
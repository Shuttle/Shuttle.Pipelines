namespace Shuttle.Pipelines.Tests;

public class MockTransactionObserver :
    IPipelineObserver<MockPipelineEvent1>,
    IPipelineObserver<MockPipelineEvent2>
{
    public bool MockPipelineEvent1HadTransactionScope { get; private set; }
    public bool MockPipelineEvent2HadTransactionScope { get; private set; }

    public Task ExecuteAsync(IPipelineContext<MockPipelineEvent1> pipelineContext, CancellationToken cancellationToken = default)
    {
        MockPipelineEvent1HadTransactionScope = System.Transactions.Transaction.Current != null;

        return Task.CompletedTask;
    }

    public Task ExecuteAsync(IPipelineContext<MockPipelineEvent2> pipelineContext, CancellationToken cancellationToken = default)
    {
        MockPipelineEvent2HadTransactionScope = System.Transactions.Transaction.Current != null;

        return Task.CompletedTask;
    }
}
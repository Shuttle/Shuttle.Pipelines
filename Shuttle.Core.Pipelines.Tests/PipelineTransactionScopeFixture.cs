using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Core.Pipelines.Tests;

[TestFixture]
public class PipelineTransactionScopeFixture
{
    private static Pipeline GetPipeline(ServiceProvider? serviceProvider = null)
    {
        var pipelineOptions = Options.Create(new PipelineOptions());
        var transactionScopeOptions = Options.Create(new TransactionScopeOptions());

        return new(pipelineOptions, transactionScopeOptions, new TransactionScopeFactory(transactionScopeOptions), serviceProvider ?? new Mock<IServiceProvider>().Object);
    }

    [Test]
    public async Task Should_be_able_to_use_transaction_scope_async()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MockTransactionObserver>();

        var serviceProvider = services.BuildServiceProvider();

        var pipeline = GetPipeline(serviceProvider);

        pipeline
            .AddStage("Transaction")
            .WithEvent<MockPipelineEvent1>()
            .WithTransactionScope();

        pipeline
            .AddStage("Stage")
            .WithEvent<MockPipelineEvent2>();

        pipeline.AddObserver<MockTransactionObserver>();

        await pipeline.ExecuteAsync(CancellationToken.None);

        var observer = serviceProvider.GetRequiredService<MockTransactionObserver>();

        Assert.That(observer.MockPipelineEvent1HadTransactionScope, Is.True);
        Assert.That(observer.MockPipelineEvent2HadTransactionScope, Is.False);
    }
}
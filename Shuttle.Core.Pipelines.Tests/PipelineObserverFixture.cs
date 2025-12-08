using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Core.Pipelines.Tests;

[TestFixture]
public class PipelineObserverFixture
{
    private static Pipeline.Context GetPipelineContext(ServiceProvider? serviceProvider = null)
    {
        return new()
        {
            PipelineOptions = Options.Create(new PipelineOptions()),
            ServiceProvider = serviceProvider ?? new Mock<IServiceProvider>().Object,
            TransactionScopeOptions = Options.Create(new TransactionScopeOptions()),
            TransactionScopeConfiguration = new TransactionScopeConfiguration(),
            TransactionScopeFactory = new TransactionScopeFactory(Options.Create(new TransactionScopeOptions()))
        };
    }

    [Test]
    public async Task Should_be_able_to_execute_a_type_based_pipeline_async()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MockAuthenticateObserver>();

        var serviceProvider = services.BuildServiceProvider();

        var pipeline = new Pipeline(new()
        {
            PipelineOptions = Options.Create(new PipelineOptions()),
            ServiceProvider = serviceProvider,
            TransactionScopeOptions = Options.Create(new TransactionScopeOptions()),
            TransactionScopeConfiguration = new TransactionScopeConfiguration(),
            TransactionScopeFactory = new TransactionScopeFactory(Options.Create(new TransactionScopeOptions()))
        });

        pipeline
            .AddStage("Stage")
            .WithEvent<MockPipelineEvent1>()
            .WithEvent<MockPipelineEvent2>()
            .WithEvent<MockPipelineEvent3>();

        pipeline.AddObserver<MockAuthenticateObserver>();

        await pipeline.ExecuteAsync(CancellationToken.None);

        var observer = serviceProvider.GetService<MockAuthenticateObserver>();

        Assert.That(observer!.CallSequence, Is.EqualTo("123"));
    }

    [Test]
    public async Task Should_be_able_to_execute_a_valid_pipeline_async()
    {
        var pipeline = new Pipeline(GetPipelineContext());

        pipeline
            .AddStage("Stage")
            .WithEvent<MockPipelineEvent1>()
            .WithEvent<MockPipelineEvent2>()
            .WithEvent<MockPipelineEvent3>();

        var observer = new MockAuthenticateObserver();

        pipeline.AddObserver(observer);

        await pipeline.ExecuteAsync(CancellationToken.None);

        Assert.That(observer.CallSequence, Is.EqualTo("123"));
    }

    [Test]
    public async Task Should_be_able_to_register_events_after_existing_event_async()
    {
        var pipeline = new Pipeline(GetPipelineContext());

        pipeline.AddStage("Stage")
            .WithEvent<MockPipelineEvent3>()
            .AfterEvent<MockPipelineEvent3>().Add<MockPipelineEvent2>()
            .AfterEvent<MockPipelineEvent2>().Add<MockPipelineEvent1>();

        var observer = new MockAuthenticateObserver();

        pipeline.AddObserver(observer);

        await pipeline.ExecuteAsync(CancellationToken.None);

        Assert.That(observer.CallSequence, Is.EqualTo("321"));
    }

    [Test]
    public async Task Should_be_able_to_register_events_before_existing_event_async()
    {
        var pipeline = new Pipeline(GetPipelineContext());

        pipeline.AddStage("Stage")
            .WithEvent<MockPipelineEvent1>();

        pipeline.GetStage("Stage").BeforeEvent<MockPipelineEvent1>().Add<MockPipelineEvent2>();
        pipeline.GetStage("Stage").BeforeEvent<MockPipelineEvent2>().Add<MockPipelineEvent3>();

        var observer = new MockAuthenticateObserver();

        pipeline.AddObserver(observer);

        await pipeline.ExecuteAsync(CancellationToken.None);

        Assert.That(observer.CallSequence, Is.EqualTo("321"));
    }

    [Test]
    public void Should_fail_on_attempt_to_register_events_after_non_existent_event()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Pipeline(GetPipelineContext())
                .AddStage("Stage")
                .AfterEvent<MockPipelineEvent1>()
                .Add<MockPipelineEvent2>());
    }

    [Test]
    public void Should_fail_on_attempt_to_register_events_before_non_existent_event()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Pipeline(GetPipelineContext())
                .AddStage("Stage")
                .BeforeEvent<MockPipelineEvent1>()
                .Add<MockPipelineEvent2>());
    }

    [Test]
    public async Task Should_be_able_to_call_an_interfaced_observer_async()
    {
        var pipeline = new Pipeline(GetPipelineContext());

        pipeline.AddStage("Stage")
            .WithEvent<MockPipelineEvent1>();

        var interfacedObserver = new InterfacedObserver();

        pipeline.AddObserver(interfacedObserver);

        await pipeline.ExecuteAsync(CancellationToken.None);

        Assert.That(interfacedObserver.Called, Is.True);
    }

    [Test]
    public void Should_be_able_to_use_scoped_ambient_context_state_async()
    {
        var ambientDataService = new AmbientDataService();
        var pipeline = new AmbientDataPipeline(GetPipelineContext(), ambientDataService);

        Assert.That(async () =>
        {
            ambientDataService.BeginScope();

            await pipeline.ExecuteAsync(CancellationToken.None);
        }, Throws.Nothing);
    }

    [Test]
    public void Should_be_not_able_to_use_ambient_context_state_without_scope_async()
    {
        var ambientDataService = new AmbientDataService();
        var pipeline = new AmbientDataPipeline(GetPipelineContext(), ambientDataService);

        Assert.That(async () =>
        {
            await pipeline.ExecuteAsync(CancellationToken.None);
        }, Throws.TypeOf<PipelineException>());
    }
}
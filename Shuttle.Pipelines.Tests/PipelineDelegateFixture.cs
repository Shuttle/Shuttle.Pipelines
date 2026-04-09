using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Shuttle.Pipelines.Tests;

internal static class PipelineExtensions
{
    private static void CallSequence(IPipelineContext pipelineContext, string number)
    {
        pipelineContext.Pipeline.State.Replace("CallSequence", (pipelineContext.Pipeline.State.Get<string>("CallSequence") ?? string.Empty) + number);
    }

    public static void MapObservers(this Pipeline pipeline)
    {
        pipeline.AddObserver(async (IPipelineContext<MockPipelineEvent1> pipelineContext) =>
        {
            CallSequence(pipelineContext, "1");

            await Task.CompletedTask;
        });

        pipeline.AddObserver(async (IPipelineContext<MockPipelineEvent2> pipelineContext) =>
        {
            CallSequence(pipelineContext, "2");

            await Task.CompletedTask;
        });

        pipeline.AddObserver(async (IPipelineContext<MockPipelineEvent3> pipelineContext) =>
        {
            CallSequence(pipelineContext, "3");

            await Task.CompletedTask;
        });
    }
}

[TestFixture]
public class PipelineDelegateFixture
{
    [Test]
    public async Task Should_be_able_to_execute_a_type_based_pipeline_async()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MockAuthenticateObserver>();

        var serviceProvider = services.BuildServiceProvider();

        var pipeline = GetPipeline(serviceProvider);

        pipeline
            .AddStage("Stage")
            .WithEvent<MockPipelineEvent1>()
            .WithEvent<MockPipelineEvent2>()
            .WithEvent<MockPipelineEvent3>();

        var callSequence = string.Empty;

        pipeline.AddObserver(async (IPipelineContext<MockPipelineEvent1> _) =>
        {
            callSequence += "1";
            await Task.CompletedTask;
        });

        pipeline.AddObserver(async (IPipelineContext<MockPipelineEvent2> _) =>
        {
            callSequence += "2";
            await Task.CompletedTask;
        });

        pipeline.AddObserver(async (IPipelineContext<MockPipelineEvent3> _) =>
        {
            callSequence += "3";
            await Task.CompletedTask;
        });

        await pipeline.ExecuteAsync(CancellationToken.None);

        Assert.That(callSequence, Is.EqualTo("123"));
    }

    private static Pipeline GetPipeline(ServiceProvider? serviceProvider = null)
    {
        var pipelineOptions = Options.Create(new PipelineOptions());

        return new(pipelineOptions, serviceProvider ?? new Mock<IServiceProvider>().Object);
    }

    [Test]
    public async Task Should_be_able_to_register_events_after_existing_event_async()
    {
        var pipeline = GetPipeline();

        pipeline.AddStage("Stage")
            .WithEvent<MockPipelineEvent3>()
            .AfterEvent<MockPipelineEvent3>().Add<MockPipelineEvent2>()
            .AfterEvent<MockPipelineEvent2>().Add<MockPipelineEvent1>();

        pipeline.MapObservers();

        await pipeline.ExecuteAsync();

        Assert.That(pipeline.State.Get<string>("CallSequence"), Is.EqualTo("321"));
    }

    [Test]
    public async Task Should_be_able_to_register_events_before_existing_event_async()
    {
        var pipeline = GetPipeline();

        pipeline.AddStage("Stage")
            .WithEvent<MockPipelineEvent1>();

        pipeline.GetStage("Stage").BeforeEvent<MockPipelineEvent1>().Add<MockPipelineEvent2>();
        pipeline.GetStage("Stage").BeforeEvent<MockPipelineEvent2>().Add<MockPipelineEvent3>();

        pipeline.MapObservers();

        await pipeline.ExecuteAsync(CancellationToken.None);

        Assert.That(pipeline.State.Get<string>("CallSequence"), Is.EqualTo("321"));
    }

    [Test]
    public async Task Should_be_able_to_register_multiple_delegates_for_the_same_event_type()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MockAuthenticateObserver>();

        var pipeline = GetPipeline();

        pipeline
            .AddStage("Stage")
            .WithEvent<MockPipelineEvent1>()
            .WithEvent<MockPipelineEvent2>()
            .WithEvent<MockPipelineEvent3>();

        var callSequence = string.Empty;

        pipeline.AddObserver(async (IPipelineContext<MockPipelineEvent1> _) =>
        {
            callSequence += "1";
            await Task.CompletedTask;
        });

        pipeline.AddObserver(async (IPipelineContext<MockPipelineEvent1> _) =>
        {
            callSequence += "1";
            await Task.CompletedTask;
        });

        pipeline.AddObserver(async (IPipelineContext<MockPipelineEvent1> _) =>
        {
            callSequence += "1";
            await Task.CompletedTask;
        });

        await pipeline.ExecuteAsync(CancellationToken.None);

        Assert.That(callSequence, Is.EqualTo("111"));
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Extensions.Options;

namespace Shuttle.Pipelines.Tests;

[TestFixture]
public class AddPipelinesFixture
{
    [Test]
    public async Task Should_merge_async_event_handlers_from_multiple_AddThreading_calls()
    {
        var invokedA = 0;
        var invokedB = 0;

        AsyncEventHandler<PipelineEventArgs> handlerA = (_, _) =>
        {
            Interlocked.Increment(ref invokedA);
            return Task.CompletedTask;
        };

        AsyncEventHandler<PipelineEventArgs> handlerB = (_, _) =>
        {
            Interlocked.Increment(ref invokedB);
            return Task.CompletedTask;
        };

        var provider = new ServiceCollection()
            .AddPipelines(options =>
            {
                options.PipelineAborted += handlerA;
            })
            .Services
            .AddPipelines(options =>
            {
                options.PipelineAborted += handlerB;
            })
            .Services
            .BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<PipelineOptions>>().Value;

        Assert.That(options.PipelineAborted.Count, Is.EqualTo(2));

        await options.PipelineAborted.InvokeAsync(new(new Mock<IPipeline>().Object), CancellationToken.None);

        Assert.That(invokedA, Is.EqualTo(1));
        Assert.That(invokedB, Is.EqualTo(1));
    }

    [Test]
    public void Should_not_add_same_async_event_handler_twice()
    {
        var invoked = 0;

        AsyncEventHandler<PipelineEventArgs> handler = (_, _) =>
        {
            Interlocked.Increment(ref invoked);
            return Task.CompletedTask;
        };

        var provider = new ServiceCollection()
            .AddPipelines(options =>
            {
                options.PipelineAborted += handler;
            })
            .Services
            .AddPipelines(options =>
            {
                options.PipelineAborted += handler;
            })
            .Services
            .BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<PipelineOptions>>().Value;

        Assert.That(options.PipelineAborted.Count, Is.EqualTo(1));
    }

    [Test]
    public void Should_preserve_async_event_instance_across_multiple_configure_calls()
    {
        AsyncEvent<PipelineEventArgs>? instanceFromFirstConfigure = null;

        var provider = new ServiceCollection()
            .AddPipelines(options =>
            {
                instanceFromFirstConfigure = options.PipelineAborted;
            })
            .Services
            .AddPipelines(options =>
            {
                Assert.That(options.PipelineAborted, Is.SameAs(instanceFromFirstConfigure));
            })
            .Services
            .BuildServiceProvider();

        _ = provider.GetRequiredService<IOptions<PipelineOptions>>().Value;
    }
}
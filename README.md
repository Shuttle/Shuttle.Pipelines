# Shuttle.Core.Pipelines

Observable event-based pipelines based broadly on pipes and filters.

## Installation

```bash
dotnet add package Shuttle.Core.Pipelines
```

## Configuration

In order to more easily make use of pipelines an implementation of the `IPipelineFactory` should be used. The following will register the `PipelineFactory` implementation:

```c#
services.AddPipelines(builder => {
    builder.AddAssembly(assembly);
});
```

This will register the `IPipelineFactory` as `Scoped` and, using the builder, add all `IPipeline` implementations as `Transient` and all `IPipelineObserver` implementations as `Scoped`.

Since pipelines are quite frequently extended by adding observers, the recommended pattern is to make use of an `IHostedService` implementation that accepts the `IPipelineFactory` dependency:

```c#
public class CustomHostedService : IHostedService
{
    private readonly Type _pipelineType = typeof(InterestingPipeline);
    private readonly PipelineOptions _pipelineOptions; // Keep reference to unsubscribe if needed

    public CustomHostedService(IOptions<PipelineOptions> pipelineOptions)
    {
        _pipelineOptions = Guard.AgainstNull(Guard.AgainstNull(pipelineOptions).Value);

        _pipelineOptions.PipelineCreated += OnPipelineCreated;
    }

    private async Task OnPipelineCreated(object sender, PipelineEventArgs e)
    {
        if (e.Pipeline.GetType() != _pipelineType)
        {
            return;
        }

        e.Pipeline.AddObserver(new SomeObserver());

        await Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _pipelineOptions.PipelineCreated -= OnPipelineCreated;

        await Task.CompletedTask;
    }
}
```

Typically you would also have a way to register the above `CustomHostedService` with the `IServiceCollection`:

```c#
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomPipelineObserver(this IServiceCollection services)
    {
        services.AddHostedService<CustomHostedService>();

        return services;
    }
}
```

The above is a rather naive example but it should give you an idea of how to extend pipelines using the `IPipelineFactory` and `IHostedService` implementations.

## Overview

A `Pipeline` is a variation of the pipes and filters pattern and consists of 1 or more stages that each contain one or more event types. When the pipeline is executed each event in each stage is raised in the order that they were registered. One or more observers should be registered to handle the relevant event(s).

Each `Pipeline` always has its own state that is simply a name/value pair with some convenience methods to get and set/replace values. The `State` class will use the full type name of the object as a key should none be specified:

``` c#
var state = new State();
var list = new List<string> {"item-1"};

state.Add(list); // key = System.Collections.Generic.List`1[[System.String...]]
state.Add("my-key", "my-key-value");

Console.WriteLine(state.Get<List<string>>()[0]);
Console.Write(state.Get<string>("my-key"));
```

The `Pipeline` class has a `AddStage` method that will return a `PipelineStage` instance. The `PipelineStage` instance has a `WithEvent` method that will return a `PipelineStage` instance. This allows for a fluent interface to register events for a pipeline:

### IPipelineObserver

The `IPipelineObserver` interface is used to define the observer that will handle the events:

``` c#
public interface IPipelineObserver<TPipelineEvent> : IPipelineObserver where TPipelineEvent : class
{
    Task ExecuteAsync(IPipelineContext<TPipelineEvent> pipelineContext, CancellationToken cancellationToken = default);
}
```

The `ExecuteAsync` method is used for processing the event.

### Pipeline Context

The `IPipelineContext<T>` provides access to the `Pipeline` instance, allowing observers to interact with the pipeline state or abort the pipeline.

```c#
public interface IPipelineContext<out T> where T : class
{
    IPipeline Pipeline { get; }
}
```

## Example

Events should be simple classes (markers) as they do not carry data themselves; data is shared via the Pipeline State.

We will use the following events:

``` c#
public class OnAddCharacterA;
public class OnAddCharacter;
```

In order for the pipeline to process the events we will have to define one or more observers to handle the events.

``` c#
public class CharacterPipelineObserver : 
    IPipelineObserver<OnAddCharacterA>,
    IPipelineObserver<OnAddCharacter>
{
    public Task ExecuteAsync(IPipelineContext<OnAddCharacterA> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = pipelineContext.Pipeline.State;
        var value = state.Get<string>("value");

        value = string.Format("{0}-A", value);

        state.Replace("value", value);

        return Task.CompletedTask;
    }

    public Task ExecuteAsync(IPipelineContext<OnAddCharacter> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = pipelineContext.Pipeline.State;
        var value = state.Get<string>("value");
        var character = state.Get<char>("character");

        value = string.Format("{0}-{1}", value, character);

        state.Replace("value", value);

        return Task.CompletedTask;
    }
}
```

Next we will define the pipeline itself:

``` c#
var pipeline = new Pipeline(pipelineDependencies); // Dependencies injected via DI in real app

pipeline.AddStage("process")
	.WithEvent<OnAddCharacterA>()
	.WithEvent<OnAddCharacter>();

pipeline.AddObserver(new CharacterPipelineObserver());

pipeline.State.Add("value", "start");
pipeline.State.Add("character", 'Z');

await pipeline.ExecuteAsync();

Console.WriteLine(pipeline.State.Get<string>("value")); // outputs start-A-Z
```

## Advanced Features

### Transaction Scope

Pipelines support `TransactionScope` which can be started at the beginning of a stage.

```c#
pipeline.AddStage("DatabaseOperation")
    .WithEvent<OnOperation>()
    .WithTransactionScope(); // Starts a TransactionScope for this stage
```

You can also explicitly manage the scope using `CompleteTransactionScope` and `DisposeTransactionScope` events, though `WithTransactionScope()` is the recommended extension method.

### Event Ordering

You can inject events into an existing pipeline stage relative to other events:

```c#
// Add MyEvent before ExistingEvent
pipeline.GetStage("Process")
    .BeforeEvent<ExistingEvent>().Add<MyEvent>();

// Add MyEvent after ExistingEvent
pipeline.GetStage("Process")
    .AfterEvent<ExistingEvent>().Add<MyEvent>();
```

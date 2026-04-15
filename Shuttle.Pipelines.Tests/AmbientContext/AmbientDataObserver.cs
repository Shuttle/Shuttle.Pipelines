namespace Shuttle.Pipelines.Tests;

public class AmbientDataObserver(IAmbientDataService ambientDataService) :
    IPipelineObserver<OnAddValue>,
    IPipelineObserver<OnGetValue>,
    IPipelineObserver<OnRemoveValue>
{
    public async Task ExecuteAsync(IPipelineContext<OnAddValue> pipelineContext, CancellationToken cancellationToken = default)
    {
        ambientDataService.Add("A");

        await Task.CompletedTask;
    }

    public async Task ExecuteAsync(IPipelineContext<OnGetValue> pipelineContext, CancellationToken cancellationToken = default)
    {
        Console.WriteLine(ambientDataService.Current);

        await Task.CompletedTask;
    }

    public async Task ExecuteAsync(IPipelineContext<OnRemoveValue> pipelineContext, CancellationToken cancellationToken = default)
    {
        ambientDataService.Remove("A");

        await Task.CompletedTask;
    }
}

public class OnRemoveValue
{
}

public class OnGetValue
{
}

public class OnAddValue
{
}
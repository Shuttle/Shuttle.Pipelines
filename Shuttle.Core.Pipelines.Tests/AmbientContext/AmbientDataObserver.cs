namespace Shuttle.Core.Pipelines.Tests;

public class AmbientDataObserver(IAmbientDataService ambientDataService) :
    IPipelineObserver<OnAddValue>,
    IPipelineObserver<OnGetValue>,
    IPipelineObserver<OnRemoveValue>
{
    public async Task ExecuteAsync(IPipelineContext<OnAddValue> pipelineContext)
    {
        ambientDataService.Add("A");

        await Task.CompletedTask;
    }

    public async Task ExecuteAsync(IPipelineContext<OnGetValue> pipelineContext)
    {
        Console.WriteLine(ambientDataService.Current);

        await Task.CompletedTask;
    }

    public async Task ExecuteAsync(IPipelineContext<OnRemoveValue> pipelineContext)
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
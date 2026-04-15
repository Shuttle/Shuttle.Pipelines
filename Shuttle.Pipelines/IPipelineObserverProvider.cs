namespace Shuttle.Pipelines;

internal interface IPipelineObserverProvider
{
    IPipelineObserver GetObserverInstance();
    Type GetObserverType();
}
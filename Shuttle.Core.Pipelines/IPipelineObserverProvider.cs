namespace Shuttle.Core.Pipelines;

internal interface IPipelineObserverProvider
{
    IPipelineObserver GetObserverInstance();
    Type GetObserverType();
}
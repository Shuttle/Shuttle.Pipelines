namespace Shuttle.Core.Pipelines;

internal class ServiceProviderPipelineObserverProvider(IServiceProvider serviceProvider, Type type) : IPipelineObserverProvider
{
    public IPipelineObserver GetObserverInstance()
    {
        return serviceProvider.GetService(type) as IPipelineObserver ?? throw new InvalidOperationException(string.Format(Resources.MissingPipelineObserverException, type.FullName));
    }

    public Type GetObserverType()
    {
        return type;
    }
}
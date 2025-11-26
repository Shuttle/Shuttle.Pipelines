using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

internal class InstancePipelineObserverProvider(IPipelineObserver pipelineObserver) : IPipelineObserverProvider
{
    private readonly IPipelineObserver _pipelineObserver = Guard.AgainstNull(pipelineObserver);
    private readonly Type _type = pipelineObserver.GetType();

    public IPipelineObserver GetObserverInstance()
    {
        return _pipelineObserver;
    }

    public Type GetObserverType()
    {
        return _type;
    }
}
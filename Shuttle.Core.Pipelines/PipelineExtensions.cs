using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public static class PipelineExtensions
{
    extension(IPipeline pipeline)
    {
        public IPipeline AddObserver<T>()
        {
            return Guard.AgainstNull(pipeline).AddObserver(typeof(T));
        }
    }
}
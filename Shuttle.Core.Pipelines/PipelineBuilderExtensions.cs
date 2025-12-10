using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public static class PipelineOptionsExtensions
{
    extension(PipelineOptions builder)
    {
        public PipelineOptions UseTransactionScope<TPipeline>(string stageName) where TPipeline : IPipeline
        {
            return Guard.AgainstNull(builder).UseTransactionScope(typeof(TPipeline), Guard.AgainstEmpty(stageName));
        }
    }
}
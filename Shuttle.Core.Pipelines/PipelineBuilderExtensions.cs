using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public static class PipelineBuilderExtensions
{
    extension(PipelineBuilder builder)
    {
        public PipelineBuilder AddTransactionScope<TPipeline>(string stageName) where TPipeline : IPipeline
        {
            return Guard.AgainstNull(builder).AddTransactionScope(typeof(TPipeline), Guard.AgainstEmpty(stageName));
        }
    }
}
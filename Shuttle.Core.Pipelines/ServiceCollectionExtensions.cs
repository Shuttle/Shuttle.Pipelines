using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPipelines(Action<PipelineBuilder>? builder = null)
        {
            Guard.AgainstNull(services);

            var pipelineProcessingBuilder = new PipelineBuilder(services);

            builder?.Invoke(pipelineProcessingBuilder);

            services.AddOptions<PipelineOptions>().Configure(options =>
            {
                options.ReusePipelines = pipelineProcessingBuilder.Options.ReusePipelines;

                options.PipelineCompleted = pipelineProcessingBuilder.Options.PipelineCompleted;
                options.PipelineCreated = pipelineProcessingBuilder.Options.PipelineCreated;
                options.PipelineRecursiveException = pipelineProcessingBuilder.Options.PipelineRecursiveException;
                options.PipelineStarting = pipelineProcessingBuilder.Options.PipelineStarting;
                options.StageCompleted = pipelineProcessingBuilder.Options.StageCompleted;
                options.StageStarting = pipelineProcessingBuilder.Options.StageStarting;

                options.TransactionScopePipelineStageName = pipelineProcessingBuilder.Options.TransactionScopePipelineStageName;
            });

            services.TryAddTransient<IPipelineFactory, PipelineFactory>();
            services.AddTransient<IPipelineDependencies, PipelineDependencies>();

            return services;
        }
    }
}
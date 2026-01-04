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

            services.AddOptions<PipelineOptions>();

            var pipelineProcessingBuilder = new PipelineBuilder(services);

            builder?.Invoke(pipelineProcessingBuilder);

            services.TryAddScoped<IPipelineFactory, PipelineFactory>();
            services.TryAddScoped<IPipelineDependencies, PipelineDependencies>();

            return services;
        }
    }
}
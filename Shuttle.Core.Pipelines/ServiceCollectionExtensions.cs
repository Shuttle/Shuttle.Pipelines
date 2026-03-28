using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPipelines(Action<PipelineBuilder>? builder = null)
        {
            Guard.AgainstNull(services);

            services.AddOptions();

            var pipelineProcessingBuilder = new PipelineBuilder(services);

            builder?.Invoke(pipelineProcessingBuilder);

            return services;
        }
    }
}
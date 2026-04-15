using Microsoft.Extensions.DependencyInjection;
using Shuttle.Contract;

namespace Shuttle.Pipelines;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public PipelineBuilder AddPipelines(Action<PipelineOptions>? configureOptions = null)
        {
            var builder = new PipelineBuilder(Guard.AgainstNull(services));

            services.AddOptions<PipelineOptions>().Configure(options =>
            {
                configureOptions?.Invoke(options);
            });

            return builder;
        }
    }
}
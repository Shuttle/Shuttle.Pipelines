using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public PipelineBuilder AddPipelines(Action<PipelineOptions>? configureOptions = null)
        {
            var builder = new PipelineBuilder(Guard.AgainstNull(services));

            services.AddOptions();
            services.AddOptions<PipelineOptions>().Configure(options =>
            {
                configureOptions?.Invoke(options);
            });

            return builder;
        }
    }
}
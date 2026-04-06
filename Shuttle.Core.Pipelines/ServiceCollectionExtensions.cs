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

            services.AddOptions<PipelineOptions>().Configure(options =>
            {
                configureOptions?.Invoke(options);
            });

            services.AddScoped<IPipelineState, PipelineState>();

            return builder;
        }
    }
}
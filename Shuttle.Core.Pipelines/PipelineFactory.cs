using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class PipelineFactory(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider) : IPipelineFactory
{
    private readonly PipelineOptions _pipelineOptions = Guard.AgainstNull(pipelineOptions).Value;
    private readonly IServiceProvider _serviceProvider = Guard.AgainstNull(serviceProvider);

    public async Task<TPipeline> GetPipelineAsync<TPipeline>(CancellationToken cancellationToken = default) where TPipeline : IPipeline
    {
        var pipeline = _serviceProvider.GetRequiredService<TPipeline>();

        if (pipeline == null)
        {
            throw new InvalidOperationException(string.Format(Resources.NullPipelineException, typeof(TPipeline).FullName));
        }

        await _pipelineOptions.PipelineCreated.InvokeAsync(new(pipeline), cancellationToken);

        return pipeline;
    }
}
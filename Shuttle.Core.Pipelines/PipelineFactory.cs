using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class PipelineFactory(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider, ILogger<PipelineFactory>? logger = null) : IPipelineFactory
{
    private readonly ILogger<PipelineFactory> _logger = logger ?? NullLogger<PipelineFactory>.Instance;
    private readonly PipelineOptions _pipelineOptions = Guard.AgainstNull(pipelineOptions).Value;
    private readonly IServiceProvider _serviceProvider = Guard.AgainstNull(serviceProvider);

    public async Task<TPipeline> GetPipelineAsync<TPipeline>(CancellationToken cancellationToken = default) where TPipeline : IPipeline
    {
        var pipeline = _serviceProvider.GetRequiredService<TPipeline>();

        if (pipeline == null)
        {
            throw new InvalidOperationException(string.Format(Resources.NullPipelineException, typeof(TPipeline).FullName));
        }

        LogMessage.PipelineCreated<TPipeline>(_logger);

        await _pipelineOptions.PipelineCreated.InvokeAsync(new(pipeline), cancellationToken);

        return pipeline;
    }
}
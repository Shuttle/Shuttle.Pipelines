using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class PipelineFactory(IOptions<PipelineOptions> pipelineOptions, IServiceProvider serviceProvider)
    : IPipelineFactory
{
    private readonly IServiceProvider _serviceProvider = Guard.AgainstNull(serviceProvider);
    private ReusableObjectPool<object> _pool = new();
    private readonly PipelineOptions _pipelineOptions = Guard.AgainstNull(pipelineOptions).Value;

    public async Task<TPipeline> GetPipelineAsync<TPipeline>(CancellationToken cancellationToken = default) where TPipeline : IPipeline
    {
        var pipeline = _pool.Get(typeof(TPipeline));

        if (pipeline == null)
        {
            var type = typeof(TPipeline);

            pipeline = (TPipeline)_serviceProvider.GetRequiredService(type);

            if (pipeline == null)
            {
                throw new InvalidOperationException(string.Format(Resources.NullPipelineException, type.FullName));
            }

            if (_pool.Contains(pipeline))
            {
                throw new InvalidOperationException(string.Format(Resources.DuplicatePipelineInstanceException, type.FullName));
            }

            await _pipelineOptions.PipelineCreated.InvokeAsync(new((TPipeline)pipeline), cancellationToken);
        }
        else
        {
            await _pipelineOptions.PipelineObtained.InvokeAsync(new((TPipeline)pipeline), cancellationToken);
        }

        return (TPipeline)pipeline;
    }

    public async Task ReleasePipelineAsync(IPipeline pipeline, CancellationToken cancellationToken)
    {
        if (!_pipelineOptions.ReusePipelines)
        {
            return;
        }

        Guard.AgainstNull(pipeline);

        _pool.Release(pipeline);

        await _pipelineOptions.PipelineReleased.InvokeAsync(new(pipeline), cancellationToken);
    }

    public void Flush()
    {
        _pool.Dispose();

        _pool = new();
    }
}
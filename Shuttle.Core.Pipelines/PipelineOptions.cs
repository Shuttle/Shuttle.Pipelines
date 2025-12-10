using Shuttle.Core.Contract;
using Shuttle.Extensions.Options;

namespace Shuttle.Core.Pipelines;

public class PipelineOptions
{
    private readonly Dictionary<Type, List<string>> _pipelineStageName = new();

    public AsyncEvent<PipelineEventArgs> PipelineCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineCreated { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineObtained { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineRecursiveException { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineReleased { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineStarting { get; set; } = new();
    public bool ReusePipelines { get; set; } = true;
    public AsyncEvent<PipelineEventArgs> StageCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> StageStarting { get; set; } = new();
    public AsyncEvent<TransactionScopeEventArgs> TransactionScopeStarting { get; set; } = new();

    public PipelineOptions UseTransactionScope(Type pipelineType, string stageName)
    {
        Guard.AgainstNull(pipelineType);
        Guard.AgainstEmpty(stageName);

        if (!_pipelineStageName.ContainsKey(pipelineType))
        {
            _pipelineStageName[pipelineType] = [];
        }

        if (!_pipelineStageName[pipelineType].Contains(stageName))
        {
            _pipelineStageName[pipelineType].Add(stageName);
        }

        return this;
    }

    public bool RequiresTransactionScope(Type pipelineType, string stageName)
    {
        Guard.AgainstNull(pipelineType);
        Guard.AgainstEmpty(stageName);

        return _pipelineStageName.TryGetValue(pipelineType, out var value) && value.Contains(stageName);
    }
}
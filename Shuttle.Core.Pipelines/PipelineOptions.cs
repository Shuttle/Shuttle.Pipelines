using Shuttle.Core.Contract;
using Shuttle.Extensions.Options;

namespace Shuttle.Core.Pipelines;

public class PipelineOptions
{
    internal Dictionary<Type, List<string>> TransactionScopePipelineStageName = new();

    public AsyncEvent<PipelineEventArgs> PipelineAborted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineCreated { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineRecursiveException { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> PipelineStarting { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> StageCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> StageStarting { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> EventCompleted { get; set; } = new();
    public AsyncEvent<PipelineEventArgs> EventStarting { get; set; } = new();
    public AsyncEvent<PipelineOptimizationEventArgs> Optimized { get; set; } = new();
    public AsyncEvent<TransactionScopeEventArgs> TransactionScopeStarting { get; set; } = new();

    public PipelineOptions UseTransactionScope(Type pipelineType, string stageName)
    {
        Guard.AgainstNull(pipelineType);
        Guard.AgainstEmpty(stageName);

        if (!TransactionScopePipelineStageName.ContainsKey(pipelineType))
        {
            TransactionScopePipelineStageName[pipelineType] = [];
        }

        if (!TransactionScopePipelineStageName[pipelineType].Contains(stageName))
        {
            TransactionScopePipelineStageName[pipelineType].Add(stageName);
        }

        return this;
    }

    public bool RequiresTransactionScope(Type pipelineType, string stageName)
    {
        Guard.AgainstNull(pipelineType);
        Guard.AgainstEmpty(stageName);

        return TransactionScopePipelineStageName.TryGetValue(pipelineType, out var value) && value.Contains(stageName);
    }
}
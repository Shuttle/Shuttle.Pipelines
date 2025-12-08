namespace Shuttle.Core.Pipelines;

public interface ITransactionScopeConfiguration
{
    void Add(Type pipelineType, string stageName);
    bool Contains(Type pipelineType);
    bool Contains(Type pipelineType, string stageName);
}
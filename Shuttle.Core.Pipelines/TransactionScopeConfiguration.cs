using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class TransactionScopeConfiguration : ITransactionScopeConfiguration
{
    private readonly Dictionary<Type, List<string>> _pipelineStageName = new();

    public void Add(Type pipelineType, string stageName)
    {
        Guard.AgainstNull(pipelineType);
        Guard.AgainstEmpty(stageName);

        if (!_pipelineStageName.ContainsKey(pipelineType))
        {
            _pipelineStageName[pipelineType] = [];
        }

        if (_pipelineStageName[pipelineType].Contains(stageName))
        {
            return;
        }

        _pipelineStageName[pipelineType].Add(stageName);
    }

    public bool Contains(Type pipelineType)
    {
        return _pipelineStageName.ContainsKey(Guard.AgainstNull(pipelineType));
    }

    public bool Contains(Type pipelineType, string stageName)
    {
        Guard.AgainstNull(pipelineType);
        Guard.AgainstEmpty(stageName);

        return _pipelineStageName.TryGetValue(pipelineType, out var value) && value.Contains(stageName);
    }
}
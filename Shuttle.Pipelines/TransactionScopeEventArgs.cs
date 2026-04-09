using System.Transactions;
using Shuttle.Contract;

namespace Shuttle.Pipelines;

public class TransactionScopeEventArgs(IPipeline pipeline, IsolationLevel isolationLevel, TimeSpan timeout) : PipelineEventArgs(pipeline)
{
    public IsolationLevel IsolationLevel { get; private set; } = Guard.AgainstUndefinedEnum<IsolationLevel>(isolationLevel);
    public TimeSpan Timeout { get; private set; } = timeout >= TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(30);

    public TransactionScopeEventArgs WithIsolationLevel(IsolationLevel isolationLevel)
    {
        IsolationLevel = Guard.AgainstUndefinedEnum<IsolationLevel>(isolationLevel);
        return this;
    }

    public TransactionScopeEventArgs WithTimeout(TimeSpan timeout)
    {
        Timeout = timeout >= TimeSpan.Zero? timeout : TimeSpan.FromSeconds(30);
        return this;
    }
}
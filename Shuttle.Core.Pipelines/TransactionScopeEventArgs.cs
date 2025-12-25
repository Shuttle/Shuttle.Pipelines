using System.Transactions;
using Shuttle.Core.Contract;

namespace Shuttle.Core.Pipelines;

public class TransactionScopeEventArgs(IPipeline pipeline, IsolationLevel isolationLevel, TimeSpan timeout)
{
    public IPipeline Pipeline { get; } = Guard.AgainstNull(pipeline);
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
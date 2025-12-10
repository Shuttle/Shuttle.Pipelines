using Shuttle.Core.TransactionScope;

namespace Shuttle.Core.Pipelines;

public interface IPipelineDependencies
{
    TransactionScopeOptions TransactionScopeOptions { get; }
    ITransactionScopeFactory TransactionScopeFactory { get; }
    IServiceProvider ServiceProvider { get; }
    PipelineOptions PipelineOptions { get; }
}
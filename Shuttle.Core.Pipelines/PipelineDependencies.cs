using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Core.Pipelines;

public class PipelineDependencies(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider) : IPipelineDependencies
{
    public ITransactionScopeFactory TransactionScopeFactory { get; } = Guard.AgainstNull(transactionScopeFactory);
    public TransactionScopeOptions TransactionScopeOptions { get; } = Guard.AgainstNull(Guard.AgainstNull(transactionScopeOptions).Value);
    public IServiceProvider ServiceProvider { get; } = Guard.AgainstNull(serviceProvider);
    public PipelineOptions PipelineOptions { get; } = Guard.AgainstNull(Guard.AgainstNull(pipelineOptions).Value);
}
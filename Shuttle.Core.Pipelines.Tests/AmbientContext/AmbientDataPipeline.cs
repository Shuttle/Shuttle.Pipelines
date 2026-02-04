using Microsoft.Extensions.Options;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Core.Pipelines.Tests;

public class AmbientDataPipeline : Pipeline
{
    public AmbientDataPipeline(IOptions<PipelineOptions> pipelineOptions, IOptions<TransactionScopeOptions> transactionScopeOptions, ITransactionScopeFactory transactionScopeFactory, IServiceProvider serviceProvider, IAmbientDataService ambientDataService)
        : base(pipelineOptions, transactionScopeOptions, transactionScopeFactory, serviceProvider)
    {
        AddStage("Pipeline")
            .WithEvent<OnAddValue>()
            .WithEvent<OnGetValue>()
            .WithEvent<OnRemoveValue>();

        AddObserver(new AmbientDataObserver(ambientDataService));
    }
}
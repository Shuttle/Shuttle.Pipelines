using Microsoft.Extensions.Logging;

namespace Shuttle.Core.Pipelines;

public static class LogMessage
{
    private static readonly Action<ILogger, string, Exception?> PipelineCreatedDelegate =
        LoggerMessage.Define<string>(LogLevel.Trace, new(1000, "PipelineCreated"), "Pipeline created for {Pipeline}"); 
    
    private static readonly Action<ILogger, string, string, string, Exception?> PipelineEventDelegate =
        LoggerMessage.Define<string, string, string>(LogLevel.Trace, new(1001, "PipelineEvent"), "Pipeline event {EventName} in pipeline {Pipeline} at stage {StageName}");

    public static void PipelineCreated<TPipeline>(ILogger logger) =>
        PipelineCreatedDelegate(logger, typeof(TPipeline).FullName!, null);

    public static void PipelineEvent(ILogger logger, string eventName, string pipeline, string stageName) =>
        PipelineEventDelegate(logger, eventName, pipeline, stageName, null);
}

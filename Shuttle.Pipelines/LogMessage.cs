using Microsoft.Extensions.Logging;

namespace Shuttle.Pipelines;

public static class LogMessage
{
    private static readonly Action<ILogger, string, string, string, Exception?> PipelineEventDelegate =
        LoggerMessage.Define<string, string, string>(LogLevel.Trace, new(1001, "PipelineEvent"), "Pipeline event {EventName} in pipeline {Pipeline} at stage {StageName}");

    public static void PipelineEvent(ILogger logger, string eventName, string pipeline, string stageName) =>
        PipelineEventDelegate(logger, eventName, pipeline, stageName, null);
}

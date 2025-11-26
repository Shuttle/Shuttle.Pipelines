namespace Shuttle.Core.Pipelines;

public class RecursiveException(string message, Exception exception) : Exception(message, exception);
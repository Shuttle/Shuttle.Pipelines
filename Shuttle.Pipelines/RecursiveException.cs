namespace Shuttle.Pipelines;

public class RecursiveException(string message, Exception exception) : Exception(message, exception);
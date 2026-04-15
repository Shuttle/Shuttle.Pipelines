namespace Shuttle.Pipelines.Tests;

public interface IAmbientDataService
{
    string Current { get; }
    void Activate(string value);
    void Add(string value);
    void Remove(string value);
}
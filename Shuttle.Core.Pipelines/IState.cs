namespace Shuttle.Core.Pipelines;

public interface IState
{
    IState Add(string key, object? value);
    IState Clear();
    bool Contains(string key);
    object? Get(string key);
    bool Remove(string key);
    IState Replace(string key, object? value);
}
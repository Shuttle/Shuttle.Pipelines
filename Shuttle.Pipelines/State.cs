using Shuttle.Contract;

namespace Shuttle.Pipelines;

public class State : IState
{
    private readonly Dictionary<string, object?> _state = new();

    public IState Clear()
    {
        _state.Clear();
        return this;
    }

    public IState Add(string key, object? value)
    {
        _state.Add(Guard.AgainstNull(key), value);
        return this;
    }

    public IState Replace(string key, object? value)
    {
        Guard.AgainstNull(key);

        _state.Remove(key);
        _state.Add(key, value);

        return this;
    }

    public object? Get(string key)
    {
        return _state.TryGetValue(Guard.AgainstNull(key), out var result) ? result : null;
    }

    public bool Contains(string key)
    {
        return _state.ContainsKey(Guard.AgainstNull(key));
    }

    public bool Remove(string key)
    {
        return _state.Remove(key);
    }
}
namespace Shuttle.Pipelines.Tests;

public class AmbientData
{
    private readonly List<string> _values = [];
    public string ActiveValue { get; private set; } = string.Empty;
    public IEnumerable<string> Values => _values.AsReadOnly();

    public void Activate(string value)
    {
        ActiveValue = value;
    }

    public void Add(string value)
    {
        if (_values.Contains(value))
        {
            throw new InvalidOperationException();
        }

        _values.Add(value);
    }

    public void Remove(string value)
    {
        if (!_values.Contains(value))
        {
            throw new InvalidOperationException();
        }

        _values.Remove(value);
    }
}
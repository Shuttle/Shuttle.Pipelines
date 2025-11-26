using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;

namespace Shuttle.Core.Pipelines;

public abstract class ReusableObjectPool
{
    protected static readonly object Lock = new();
}

public class ReusableObjectPool<TReusableObject> : ReusableObjectPool, IDisposable where TReusableObject : class
{
    private readonly Func<Type, TReusableObject>? _factoryMethod;
    private readonly Dictionary<Type, List<TReusableObject>> _pool = new();

    public ReusableObjectPool()
    {
    }

    public ReusableObjectPool(Func<Type, TReusableObject> factoryMethod)
    {
        _factoryMethod = Guard.AgainstNull(factoryMethod);
    }

    public void Dispose()
    {
        foreach (var value in _pool.Values)
        {
            value.ForEach(item =>
            {
                item.TryDispose();
            });
        }
    }

    public bool Contains(TReusableObject instance)
    {
        Guard.AgainstNull(instance);

        lock (Lock)
        {
            return _pool[instance.GetType()].Find(item => item.Equals(instance)) != null;
        }
    }

    public TReusableObject? Get(Type key)
    {
        Guard.AgainstNull(key);

        lock (Lock)
        {
            if (!_pool.TryGetValue(key, out var reusableObjects))
            {
                reusableObjects = new();
                _pool.Add(key, reusableObjects);
            }

            if (reusableObjects.Count <= 0)
            {
                return _factoryMethod?.Invoke(key);
            }

            var lastIndex = reusableObjects.Count - 1;
            var reusableObject = reusableObjects[lastIndex];

            reusableObjects.RemoveAt(lastIndex);

            return reusableObject;
        }
    }

    public void Release(TReusableObject instance)
    {
        Guard.AgainstNull(instance);

        lock (Lock)
        {
            var type = instance.GetType();

            if (!_pool.TryGetValue(type, out var reusableObjects))
            {
                reusableObjects = [];
                _pool.Add(type, reusableObjects);
            }

            reusableObjects.Add(instance);
        }
    }
}
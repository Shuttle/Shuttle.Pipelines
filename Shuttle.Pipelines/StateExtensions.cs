using Shuttle.Contract;

namespace Shuttle.Pipelines;

public static class StateExtensions
{
    public static void Add<TItem>(this IState state, TItem? value)
    {
        Guard.AgainstNull(state).Add(typeof(TItem).FullName ?? throw new InvalidOperationException(string.Format(Reflection.Resources.TypeFullNameNullException, typeof(TItem).Name)), value);
    }

    public static void Add<TItem>(this IState state, string key, TItem? value)
    {
        Guard.AgainstNull(state).Add(Guard.AgainstNull(key), value);
    }

    public static bool Contains<TItem>(this IState state)
    {
        var key = typeof(TItem).FullName ?? throw new InvalidOperationException(string.Format(Reflection.Resources.TypeFullNameNullException, typeof(TItem).Name));

        return Guard.AgainstNull(state).Contains(key);
    }

    public static TItem? Get<TItem>(this IState state, string key)
    {
        Guard.AgainstNull(key);

        var result = Guard.AgainstNull(state).Get(key);

        return result == null ? default : (TItem)result;
    }

    public static TItem? Get<TItem>(this IState state)
    {
        return Guard.AgainstNull(state).Get<TItem>(typeof(TItem).FullName ?? throw new InvalidOperationException(string.Format(Reflection.Resources.TypeFullNameNullException, typeof(TItem).Name)));
    }

    public static bool Remove<TItem>(this IState state)
    {
        var key = typeof(TItem).FullName ?? throw new InvalidOperationException(string.Format(Reflection.Resources.TypeFullNameNullException, typeof(TItem).Name));

        return Guard.AgainstNull(state).Remove(key);
    }

    public static void Replace<TItem>(this IState state, TItem? value)
    {
        var key = typeof(TItem).FullName ?? throw new InvalidOperationException(string.Format(Reflection.Resources.TypeFullNameNullException, typeof(TItem).Name));

        Guard.AgainstNull(state).Remove(key);

        state.Add(key, value);
    }

    public static void Replace<TItem>(this IState state, string key, TItem? value)
    {
        Guard.AgainstNull(key);

        Guard.AgainstNull(state).Remove(key);

        state.Add(key, value);
    }
}
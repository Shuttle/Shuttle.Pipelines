using Shuttle.Core.Contract;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Core.Pipelines;

public static class TransactionScopeStateExtensions
{
    public static ITransactionScope? GetTransactionScope(this IState state)
    {
        Guard.AgainstNull(state);

        return !state.Contains("TransactionScope") ? null : state.Get<ITransactionScope>("TransactionScope");
    }

    public static void SetTransactionScope(this IState state, ITransactionScope? scope)
    {
        Guard.AgainstNull(state).Replace("TransactionScope", scope);
    }
}
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Reflection;

namespace Shuttle.Core.Pipelines;

public class ObserverDelegate(Delegate handler, IEnumerable<Type> parameterTypes)
{
    private static readonly Type PipelineContextType = typeof(IPipelineContext<>);

    public Delegate Handler { get; } = handler;
    public bool HasParameters { get; } = parameterTypes.Any();

    public object[] GetParameters(IServiceProvider serviceProvider, object pipelineContext, CancellationToken cancellationToken)
    {
        return parameterTypes
            .Select(parameterType => parameterType == typeof(CancellationToken)
                ? cancellationToken
                : !parameterType.IsCastableTo(PipelineContextType)
                    ? serviceProvider.GetRequiredService(parameterType)
                    : pipelineContext).ToArray();
    }
}
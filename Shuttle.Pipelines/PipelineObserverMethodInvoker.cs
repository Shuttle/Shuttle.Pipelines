using System.Reflection;
using System.Reflection.Emit;
using Shuttle.Contract;

namespace Shuttle.Pipelines;

internal readonly struct PipelineObserverMethodInvoker
{
    public ObserverPosition Position { get; }
    public IPipelineObserverProvider PipelineObserverProvider { get; }

    private static readonly Type PipelineContextType = typeof(PipelineContext<>);

    private readonly AsyncInvokeHandler _asyncInvoker;

    public PipelineObserverMethodInvoker(IPipelineObserverProvider pipelineObserverProvider, MethodInfo methodInfo, ObserverPosition position)
    {
        Position = position;
        PipelineObserverProvider = Guard.AgainstNull(pipelineObserverProvider);

        var dynamicMethod = new DynamicMethod(string.Empty, typeof(Task), [typeof(object), typeof(object), typeof(CancellationToken)], PipelineContextType.Module);

        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);

        il.EmitCall(OpCodes.Callvirt, methodInfo, null);
        il.Emit(OpCodes.Ret);

        _asyncInvoker = (AsyncInvokeHandler)dynamicMethod.CreateDelegate(typeof(AsyncInvokeHandler));
    }

    public async Task InvokeAsync(object pipelineContext, CancellationToken cancellationToken)
    {
        await _asyncInvoker.Invoke(PipelineObserverProvider.GetObserverInstance(), pipelineContext, cancellationToken);
    }

    private delegate Task AsyncInvokeHandler(object observer, object pipelineContext, CancellationToken cancellationToken);
}
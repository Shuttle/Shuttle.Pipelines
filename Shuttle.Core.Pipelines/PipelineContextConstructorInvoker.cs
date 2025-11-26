using System.Reflection.Emit;

namespace Shuttle.Core.Pipelines;

internal class PipelineContextConstructorInvoker
{
    private static readonly Type PipelineContext = typeof(PipelineContext<>);

    private readonly ConstructorInvokeHandler _constructorInvoker;
    private readonly IPipeline _pipeline;
    private readonly Type _pipelineType = typeof(IPipeline);

    public PipelineContextConstructorInvoker(IPipeline pipeline, Type eventType)
    {
        _pipeline = pipeline;

        var dynamicMethod = new DynamicMethod(string.Empty, typeof(object), [typeof(object)], PipelineContext.Module);

        var il = dynamicMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);

        var contextType = PipelineContext.MakeGenericType(eventType);
        var constructorInfo = contextType.GetConstructor([_pipelineType]);

        if (constructorInfo == null)
        {
            throw new InvalidOperationException(string.Format(Resources.ContextConstructorException, contextType.FullName));
        }

        il.Emit(OpCodes.Newobj, constructorInfo);
        il.Emit(OpCodes.Ret);

        _constructorInvoker = (ConstructorInvokeHandler)dynamicMethod.CreateDelegate(typeof(ConstructorInvokeHandler));
    }

    public object Create()
    {
        return _constructorInvoker(_pipeline);
    }

    private delegate object ConstructorInvokeHandler(IPipeline pipeline);
}
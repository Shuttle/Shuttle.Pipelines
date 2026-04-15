using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shuttle.Contract;
using Shuttle.Reflection;

namespace Shuttle.Pipelines;

public class PipelineBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = Guard.AgainstNull(services);

    public PipelineBuilder AddPipelinesFrom(Assembly assembly)
    {
        return AddPipelinesFrom([Guard.AgainstNull(assembly)]);
    }

    public PipelineBuilder AddPipelinesFrom(Assembly[] assemblies)
    {
        foreach (var type in assemblies.SelectMany(assembly => assembly.FindTypesCastableTo<IPipeline>()))
        {
            if (type.IsInterface || type.IsAbstract)
            {
                continue;
            }

            var interfaceType = type.InterfaceMatching($"I{type.Name}");

            if (interfaceType != null)
            {
                Services.TryAddScoped(interfaceType, type);
            }
            else
            {
                Services.TryAddTransient(type);
            }
        }

        foreach (var type in assemblies.SelectMany(assembly => assembly.FindTypesCastableTo<IPipelineObserver>()))
        {
            if (type.IsInterface || type.IsAbstract)
            {
                continue;
            }

            var interfaceType = type.InterfaceMatching($"I{type.Name}");

            if (interfaceType != null)
            {
                Services.TryAddScoped(interfaceType, type);
            }
            else
            {
                throw new InvalidOperationException(string.Format(Resources.ObserverInterfaceMissingException, type.Name));
            }
        }

        return this;
    }
}
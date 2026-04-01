using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;

namespace Shuttle.Core.Pipelines;

public class PipelineBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = Guard.AgainstNull(services);

    public PipelineBuilder AddAssembly(Assembly assembly)
    {
        Guard.AgainstNull(assembly);

        foreach (var type in assembly.GetTypesCastableToAsync<IPipeline>().GetAwaiter().GetResult())
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

        foreach (var type in assembly.GetTypesCastableToAsync<IPipelineObserver>().GetAwaiter().GetResult())
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
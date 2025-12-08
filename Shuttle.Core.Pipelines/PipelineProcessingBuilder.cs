using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;

namespace Shuttle.Core.Pipelines;

public class PipelineBuilder(IServiceCollection services)
{
    public PipelineOptions Options
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    } = new();

    public IServiceCollection Services { get; } = Guard.AgainstNull(services);
    public ITransactionScopeConfiguration TransactionScopeConfiguration { get; } = new TransactionScopeConfiguration();

    public PipelineBuilder AddTransactionScope(Type pipelineType, string stageName)
    {
        TransactionScopeConfiguration.Add(pipelineType, stageName);

        return this;
    }
    
    public PipelineBuilder AddAssembly(Assembly assembly)
    {
        Guard.AgainstNull(assembly);

        foreach (var type in assembly.GetTypesCastableToAsync<IPipeline>().GetAwaiter().GetResult())
        {
            if (type.IsInterface || type.IsAbstract || Services.Contains(ServiceDescriptor.Transient(type, type)))
            {
                continue;
            }

            Services.AddTransient(type, type);
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
                if (Services.Contains(ServiceDescriptor.Singleton(interfaceType, type)))
                {
                    continue;
                }

                Services.AddSingleton(interfaceType, type);
            }
            else
            {
                throw new InvalidOperationException(string.Format(Resources.ObserverInterfaceMissingException, type.Name));
            }
        }

        return this;
    }
}
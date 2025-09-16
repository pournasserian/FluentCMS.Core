using FluentCMS.Providers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentCMS.Providers;

public abstract class ProviderModuleBase<TProvider, TOptions> : IProviderModule<TProvider, TOptions>
    where TProvider : class, IProvider
    where TOptions : class, new()
{
    public abstract string Area { get; }
    public abstract string DisplayName { get; }
    public Type ProviderType => typeof(TProvider);
    public Type OptionsType => typeof(TOptions);

    public virtual Type InterfaceType
    {
        get
        {
            var interfaces = typeof(TProvider).GetInterfaces()
                .Where(i => i != typeof(IProvider) && typeof(IProvider).IsAssignableFrom(i))
                .ToArray();

            if (interfaces.Length == 0)
                throw new InvalidOperationException($"Provider {typeof(TProvider).Name} must implement at least one interface that extends IProvider.");

            // Return the most specific interface (first one that's not IProvider)
            return interfaces.First();
        }
    }

    public virtual void ConfigureServices(IServiceCollection services, string providerName)
    {
        // Default implementation does nothing
        // Override in derived classes to register additional services
    }
}

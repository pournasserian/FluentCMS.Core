using FluentCMS.Providers.Repositories.Abstractions;
using FluentCMS.Providers.Repositories.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers;

public static class ProviderFeatureBuilderExtensions
{
    public static ProviderFeatureBuilder AddProviders(this IServiceCollection services, Action<ProviderDiscoveryOptions>? configure = null)
    {
        services.PrepareProviderCatalogCache(configure);
        services.AddScoped<IProviderManager, ProviderManager>();
        return new ProviderFeatureBuilder(services);
    }

    public static ProviderFeatureBuilder UseConfiguration(this ProviderFeatureBuilder providerFeatureBuilder)
    {
        providerFeatureBuilder.Services.AddScoped<IProviderRepository, ConfigurationReadOnlyProviderRepository>();

        return providerFeatureBuilder;
    }

    private static void PrepareProviderCatalogCache(this IServiceCollection services, Action<ProviderDiscoveryOptions>? configure = null)
    {
        // Configure options
        var opts = new ProviderDiscoveryOptions();
        configure?.Invoke(opts);
        services.AddSingleton(opts);
        services.AddSingleton<ProviderCatalogCache>();

        var providerDiscovery = new ProviderDiscovery(opts);
        var providerModules = providerDiscovery.GetProviderModules();
        var providerModuleCatalogCache = new ProviderModuleCatalogCache(providerModules);
        services.AddSingleton(providerModuleCatalogCache);

        foreach (var module in providerModules)
        {
            try
            {
                module.ConfigureServices(services);
            }
            catch (Exception)
            {
                if (opts.IgnoreExceptions)
                    throw;
            }
        }

        // for each interface type in the catalog, register a factory to resolve the default provider
        foreach (var (area, interfaceType) in providerModuleCatalogCache.GetRegisteredInterfaceTypes())
        {
            services.AddTransient(interfaceType, serviceProvider =>
            {
                var providerManager = serviceProvider.GetRequiredService<IProviderManager>();
                var catalog = providerManager.GetActiveByArea(area).GetAwaiter().GetResult() ??
                    throw new InvalidOperationException($"No active provider found for area '{area}'.");

                // Check if the provider's type implements the requested interface
                if (!interfaceType.IsAssignableFrom(catalog.Module.ProviderType))
                    throw new InvalidOperationException($"The active provider '{catalog.Name}' for area '{area}' does not implement the interface '{interfaceType.FullName}'.");

                // Check if provider's constructor accepts the options type
                var constructor = catalog.Module.ProviderType.GetConstructors()
                    .FirstOrDefault(ctor =>
                    {
                        return ctor.GetParameters().Any(p => p.ParameterType == catalog.Module.OptionsType);
                    });
                // If it does, create an instance using ActivatorUtilities and pass the options
                if (constructor != null)
                {
                    var provider = ActivatorUtilities.CreateInstance(serviceProvider, catalog.Module.ProviderType, catalog.Options);
                    return provider;
                }
                else
                {
                    var provider = ActivatorUtilities.CreateInstance(serviceProvider, catalog.Module.ProviderType);
                    return provider;
                }
            });
        }
    }
}

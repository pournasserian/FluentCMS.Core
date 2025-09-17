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

    private static void PrepareProviderCatalogCache(this IServiceCollection services, Action<ProviderDiscoveryOptions>? configure = null)
    {
        // Configure options
        var opts = new ProviderDiscoveryOptions();
        configure?.Invoke(opts);
        services.AddSingleton(opts);

        var providerDiscovery = new ProviderDiscovery(opts);
        var providerModules = providerDiscovery.GetProviderModules();
        var providerCatalogCache = new ProviderCatalogCache(providerModules);
        services.AddSingleton(providerCatalogCache);

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
        foreach (var (area, interfaceType) in providerCatalogCache.GetRegisteredInterfaceTypes())
        {
            services.AddTransient(interfaceType, serviceProvider =>
            {
                var providerManager = serviceProvider.GetRequiredService<IProviderManager>();
                var catalog = providerManager.GetActiveByArea(area).GetAwaiter().GetResult() ??
                    throw new InvalidOperationException($"No active provider found for area '{area}'.");
                var provider = ActivatorUtilities.CreateInstance(serviceProvider, catalog.Module.ProviderType);
                return provider;
            });
        }
    }
}

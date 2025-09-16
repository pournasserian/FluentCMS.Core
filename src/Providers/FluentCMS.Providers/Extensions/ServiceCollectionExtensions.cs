using FluentCMS.Providers.Abstractions;
using FluentCMS.Providers.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers.Extensions;

/// <summary>
/// Extension methods for configuring the provider system in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProviders(this IServiceCollection services, string connectionString, Action<ProviderDiscoveryOptions>? configure = null)
    {
        services.GetProviderModules(configure);

        // Add Entity Framework with SQLite
        services.AddDbContext<ProviderDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IProviderManager, ProviderManager>();

        return services;
    }

    private static IServiceCollection GetProviderModules(this IServiceCollection services, Action<ProviderDiscoveryOptions>? configure = null)
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
                module.ConfigureServices(services, "Default");
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
            services.AddScoped(interfaceType, serviceProvider =>
            {
                var providerManager = serviceProvider.GetRequiredService<IProviderManager>();
                var catalog = providerManager.GetActiveByArea(area).GetAwaiter().GetResult() ?? 
                    throw new InvalidOperationException($"No active provider found for area '{area}'.");
                var provider = ActivatorUtilities.CreateInstance(serviceProvider, catalog.Module.ProviderType);
                return provider;
            });
        }

        return services;
    }
}

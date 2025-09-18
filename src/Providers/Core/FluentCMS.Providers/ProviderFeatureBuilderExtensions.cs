using FluentCMS.Providers.Repositories.Abstractions;
using FluentCMS.Providers.Repositories.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

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

                var providerType = catalog.Module.ProviderType;

                // Check if the provider type has more than one constructor, then raise exception
                var constructors = providerType.GetConstructors();
                if (constructors.Length == 0)
                    throw new InvalidOperationException($"The provider type '{providerType.FullName}' has no public constructors.");

                if (constructors.Length > 1)
                    throw new InvalidOperationException($"The provider type '{providerType.FullName}' has more than one constructor. Only one constructor is allowed.");

                // Prepare arguments for the constructor
                var constructor = constructors[0];
                var argsList = new List<object>();

                if (catalog.Options != null)
                {
                    foreach (var parameter in constructor.GetParameters())
                    {
                        // Adding the options instance
                        if (parameter.ParameterType == catalog.Module.OptionsType)
                            argsList.Add(catalog.Options);

                        if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IOptions<>) && parameter.ParameterType.GetGenericArguments()[0] == catalog.Module.OptionsType)
                        {
                            var optionsCreate = typeof(Options).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => m.Name == nameof(Options.Create) && m.IsGenericMethodDefinition);
                            var createGeneric = optionsCreate.MakeGenericMethod(catalog.Module.OptionsType);
                            var ioptionsInstance = createGeneric.Invoke(null, [catalog.Options]);
                            argsList.Add(ioptionsInstance!);
                        }
                    }
                }

                return ActivatorUtilities.CreateInstance(serviceProvider, catalog.Module.ProviderType, [.. argsList]);
            });
        }
    }
}

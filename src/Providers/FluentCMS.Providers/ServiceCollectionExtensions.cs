using FluentCMS.Configuration;

namespace FluentCMS.Providers;

/// <summary>
/// Extension methods for IServiceCollection to register the provider system
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the provider system to the service collection with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="configureOptions">Action to configure provider system options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddProviderSystem(this IServiceCollection services, IConfiguration configuration, Action<ProviderSystemOptions>? configureOptions)
    {
        services.AddDbOptions<ProvidersConfiguration>(configuration, "Providers");

        // Configure options
        services.Configure<ProviderSystemOptions>(opt =>
        {
            configureOptions?.Invoke(opt);
        });

        return services;
    }
}


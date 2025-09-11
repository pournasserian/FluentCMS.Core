namespace FluentCMS.Providers;

/// <summary>
/// Extension methods for IServiceCollection to register the provider system
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the provider system to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddProviderSystem(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the provider registrar and execute it
        var registrar = new ProviderRegistrar(services, configuration);
        registrar.RegisterProviders();

        return services;
    }

    /// <summary>
    /// Adds the provider system to the service collection with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="configureOptions">Action to configure provider system options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddProviderSystem(this IServiceCollection services, IConfiguration configuration, Action<ProviderSystemOptions> configureOptions)
    {
        var options = new ProviderSystemOptions();
        configureOptions(options);

        // Configure options
        services.Configure<ProviderSystemOptions>(opt =>
        {
            opt.EnableHotReload = options.EnableHotReload;
            opt.EnableHealthChecks = options.EnableHealthChecks;
            opt.ThrowOnMissingProvider = options.ThrowOnMissingProvider;
        });

        // Register the provider registrar and execute it
        var registrar = new ProviderRegistrar(services, configuration);
        registrar.RegisterProviders();

        return services;
    }
}


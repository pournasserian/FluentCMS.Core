using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Database.Abstractions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers multi-database support with extensible provider-based configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureMapping">Action to configure database mappings using fluent API.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDatabaseManager(
        this IServiceCollection services,
        Action<IDatabaseMappingBuilder> configureMapping)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configureMapping == null)
            throw new ArgumentNullException(nameof(configureMapping));

        // Build the database provider mapping configuration
        var builder = new DatabaseMappingBuilder();
        configureMapping(builder);
        var providerMapping = builder.Build();

        // Validate that at least a default configuration is set
        if (providerMapping.GetDefaultConfiguration() == null)
        {
            throw new InvalidOperationException(
                "A default database provider must be configured. Call SetDefault() and chain with a provider method (e.g., UseSqlServer()).");
        }

        // Register the provider mapping configuration as singleton
        services.AddSingleton(providerMapping);

        // Register the resolver as scoped IDatabaseManager
        services.AddScoped<IDatabaseManager, DatabaseManagerResolver>();

        return services;
    }
}

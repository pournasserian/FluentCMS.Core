using FluentCMS.DataSeeder.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeder.Sqlite;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SqliteDatabaseManager as a scoped service in the dependency injection container.
    /// </summary>
    public static IServiceCollection AddSqliteDataSeeder(this IServiceCollection services, string connectionString, Action<SeedingOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        services.AddScoped<IDatabaseManager>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<SqliteDatabaseManager>>();
            return new SqliteDatabaseManager(connectionString, logger);
        });

        services.AddDataSeeding(configure);

        return services;
    }
}

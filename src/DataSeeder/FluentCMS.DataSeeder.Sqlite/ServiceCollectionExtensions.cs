using FluentCMS.DataSeeder.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeder.Sqlite;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SqliteDatabaseManager as a scoped service in the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the service to.</param>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddSqliteDatabaseManager(this IServiceCollection services, string connectionString)
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

        return services;
    }
}

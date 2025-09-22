using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Database.Abstractions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SqlServerDatabaseManager as a scoped service in the dependency injection container.
    /// </summary>
    public static IServiceCollection AddSqlServerManager(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        services.AddScoped<IDatabaseManager>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<SqlServerDatabaseManager>>();
            return new SqlServerDatabaseManager(connectionString, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers the SqliteDatabaseManager as a scoped service in the dependency injection container.
    /// </summary>
    public static IServiceCollection AddSqliteManager(this IServiceCollection services, string connectionString)
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

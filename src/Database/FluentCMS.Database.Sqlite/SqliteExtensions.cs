using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentCMS.Database.Abstractions;

namespace FluentCMS.Database.Sqlite;

/// <summary>
/// Extension methods for configuring SQLite database provider.
/// </summary>
public static class SqliteExtensions
{
    /// <summary>
    /// Configures the current assembly mapping to use SQLite database.
    /// </summary>
    /// <param name="builder">The assembly mapping builder.</param>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <returns>The assembly mapping builder for method chaining.</returns>
    public static IAssemblyMappingBuilder UseSqlite(this IAssemblyMappingBuilder builder, string connectionString)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        return builder.RegisterProvider("Sqlite", connectionString, (connString, serviceProvider) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SqliteDatabaseManager>>();
            return new SqliteDatabaseManager(connString, logger);
        });
    }
}

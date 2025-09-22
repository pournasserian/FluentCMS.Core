using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentCMS.Database.Abstractions;

namespace FluentCMS.Database.SqlServer;

/// <summary>
/// Extension methods for configuring SQL Server database provider.
/// </summary>
public static class SqlServerExtensions
{
    /// <summary>
    /// Configures the current assembly mapping to use SQL Server database.
    /// </summary>
    /// <param name="builder">The assembly mapping builder.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The assembly mapping builder for method chaining.</returns>
    public static IAssemblyMappingBuilder UseSqlServer(this IAssemblyMappingBuilder builder, string connectionString)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        return builder.RegisterProvider("SqlServer", connectionString, (connString, serviceProvider) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SqlServerDatabaseManager>>();
            return new SqlServerDatabaseManager(connString, logger);
        });
    }
}

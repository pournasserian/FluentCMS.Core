using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeder.Sqlite;

/// <summary>
/// SQLite specific implementation of the seeding service
/// </summary>
public class SqliteSeedingService(IServiceProvider serviceProvider, SeedingOptions options, ILogger<SqliteSeedingService> logger) : SeedingService(serviceProvider, options, logger)
{
    public override async Task EnsureSchema(DbContext context)
    {
        try
        {
            Logger?.LogInformation("Ensuring SQLite database schema exists");

            // For SQLite, we can simply ensure the database is created
            // SQLite doesn't support migrations in the same way as SQL Server
            await context.Database.EnsureCreatedAsync();

            Logger?.LogInformation("SQLite database schema ensured");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error ensuring SQLite database schema");
            throw new SeedingException("Failed to ensure SQLite database schema", ex);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Configuration.EntityFramework;

public class EfConfigurationProvider(DbContextOptions<ConfigurationDbContext> options, IConfigurationRoot configurationRoot, ILogger<EfConfigurationProvider>? logger = null) : ConfigurationProvider
{
    public override void Load()
    {
        try
        {
            logger?.LogInformation("Loading configuration from Entity Framework provider");

            using var dbContext = new ConfigurationDbContext(options, configurationRoot);

            // Check if database exists and create if needed
            if (!dbContext.Database.CanConnect())
            {
                logger?.LogInformation("Database not accessible, creating database");
                dbContext.Database.EnsureCreated();
                logger?.LogInformation("Database created successfully");
            }
            else
            {
                logger?.LogDebug("Database connection verified");
            }

            // Load configuration settings with performance optimization
            var settings = dbContext.Settings
                .AsNoTracking()
                .ToList();

            Data = settings.ToDictionary(
                c => c.Key,
                c => c.Value,
                StringComparer.OrdinalIgnoreCase);

            logger?.LogInformation("Loaded {Count} configuration settings", Data.Count);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to load configuration from database");

            // Provide empty configuration rather than crashing
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            throw new InvalidOperationException(
                "Configuration database is unavailable. Check connection string and ensure database is accessible.",
                ex);
        }
    }
}

using FluentCMS.DataSeeder.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeder;


/// <summary>
/// Factory for creating appropriate seeding service based on database provider
/// </summary>
public class SeedingServiceFactory(IServiceProvider serviceProvider, SeedingOptions options, ILoggerFactory loggerFactory)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly SeedingOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    /// <summary>
    /// Creates the appropriate seeding service based on the database provider
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>The appropriate seeding service</returns>
    public ISeedingService CreateSeedingService(DbContext context)
    {
        var providerName = context.Database.ProviderName;

        return providerName?.ToLowerInvariant() switch
        {
            //"microsoft.entityframeworkcore.sqlserver" => new SqlServerSeedingService(
            //    _serviceProvider,
            //    _options,
            //    _loggerFactory.CreateLogger<SqlServerSeedingService>()),

            "microsoft.entityframeworkcore.sqlite" => new SqliteSeedingService(
                _serviceProvider,
                _options,
                _loggerFactory.CreateLogger<SqliteSeedingService>()),

            _ => new SeedingService(
                _serviceProvider,
                _options,
                _loggerFactory.CreateLogger<SeedingService>())
        };
    }

    /// <summary>
    /// Creates the appropriate seeding service based on connection string
    /// </summary>
    /// <param name="connectionString">The database connection string</param>
    /// <returns>The appropriate seeding service</returns>
    public ISeedingService CreateSeedingService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        var lowerConnectionString = connectionString.ToLowerInvariant();

        if (lowerConnectionString.Contains("data source") && lowerConnectionString.Contains(".db"))
        {
            // SQLite connection string pattern
            return new SqliteSeedingService(
                _serviceProvider,
                _options,
                _loggerFactory.CreateLogger<SqliteSeedingService>());
        }
        else if (lowerConnectionString.Contains("server") || lowerConnectionString.Contains("data source"))
        {
            // SQL Server connection string pattern
            return new SqlServerSeedingService(
                _serviceProvider,
                _options,
                _loggerFactory.CreateLogger<SqlServerSeedingService>());
        }

        // Default to base service
        return new SeedingService(
            _serviceProvider,
            _options,
            _loggerFactory.CreateLogger<SeedingService>());
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Database.Abstractions;

/// <summary>
/// Example demonstrating the new extensible multi-database support.
/// This example shows how to use the new fluent API without hardcoded database types.
/// </summary>
public static class NewExtensibleExample
{
    /// <summary>
    /// Example service from a hypothetical Users library.
    /// </summary>
    public class UserService
    {
        private readonly IDatabaseManager _databaseManager;
        private readonly ILogger<UserService> _logger;

        public UserService(IDatabaseManager databaseManager, ILogger<UserService> logger)
        {
            _databaseManager = databaseManager;
            _logger = logger;
        }

        public async Task InitializeUserDatabase(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing user database...");
            
            // This will automatically use the database configured for this assembly
            if (!await _databaseManager.DatabaseExists(cancellationToken))
            {
                await _databaseManager.CreateDatabase(cancellationToken);
                _logger.LogInformation("User database created.");
            }

            if (!await _databaseManager.TablesExist(new[] { "Users", "Roles" }, cancellationToken))
            {
                await _databaseManager.ExecuteRaw(@"
                    CREATE TABLE Users (
                        Id INTEGER PRIMARY KEY,
                        Username TEXT NOT NULL,
                        Email TEXT NOT NULL
                    );
                    
                    CREATE TABLE Roles (
                        Id INTEGER PRIMARY KEY,
                        Name TEXT NOT NULL
                    );", cancellationToken);
                
                _logger.LogInformation("User tables created.");
            }
        }
    }

    /// <summary>
    /// Example service from a hypothetical Content library.
    /// </summary>
    public class ContentService
    {
        private readonly IDatabaseManager _databaseManager;
        private readonly ILogger<ContentService> _logger;

        public ContentService(IDatabaseManager databaseManager, ILogger<ContentService> logger)
        {
            _databaseManager = databaseManager;
            _logger = logger;
        }

        public async Task InitializeContentDatabase(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing content database...");
            
            // This will automatically use the database configured for this assembly
            if (!await _databaseManager.DatabaseExists(cancellationToken))
            {
                await _databaseManager.CreateDatabase(cancellationToken);
                _logger.LogInformation("Content database created.");
            }

            if (!await _databaseManager.TablesExist(new[] { "Pages", "Posts" }, cancellationToken))
            {
                await _databaseManager.ExecuteRaw(@"
                    CREATE TABLE Pages (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Title NVARCHAR(255) NOT NULL,
                        Content NVARCHAR(MAX)
                    );
                    
                    CREATE TABLE Posts (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Title NVARCHAR(255) NOT NULL,
                        Body NVARCHAR(MAX),
                        PublishedAt DATETIME2
                    );", cancellationToken);
                
                _logger.LogInformation("Content tables created.");
            }
        }
    }

    /// <summary>
    /// Example of how to configure the new extensible database support in Program.cs
    /// 
    /// NOTE: This is conceptual code showing the API design.
    /// To actually run this code, you would need to:
    /// 1. Add reference to FluentCMS.Database.SqlServer package
    /// 2. Add reference to FluentCMS.Database.Sqlite package
    /// 3. Add the appropriate using statements:
    ///    using FluentCMS.Database.SqlServer;
    ///    using FluentCMS.Database.Sqlite;
    /// </summary>
    public static string ConceptualConfigurationExample = @"
// In Program.cs, with provider packages referenced:

var defaultConn = configuration.GetConnectionString(""DefaultConnection"");
var usersConn = configuration.GetConnectionString(""UsersConnection"");
var contentConn = configuration.GetConnectionString(""ContentConnection"");

services.AddDatabaseManager(options =>
{
    // Set default database - extension methods come from provider libraries
    options.SetDefault().UseSqlServer(defaultConn);
    
    // Map specific assemblies to different databases
    options.MapAssembly<UserService>().UseSqlite(usersConn);
    options.MapAssembly<ContentService>().UseSqlServer(contentConn);
    
    // Future providers can be added without modifying this library:
    // options.MapAssembly<LoggingService>().UsePostgreSQL(logsConn);
    // options.MapAssembly<AnalyticsService>().UseMongoDB(analyticsConn);
});

// Register application services
services.AddScoped<UserService>();
services.AddScoped<ContentService>();
";

    /// <summary>
    /// Example showing the benefits of the new extensible architecture
    /// </summary>
    public static void ArchitectureBenefits()
    {
        Console.WriteLine("🎯 New Extensible Database Architecture Benefits:");
        Console.WriteLine();
        Console.WriteLine("✅ No hardcoded DatabaseType enum");
        Console.WriteLine("✅ Extension methods auto-register providers when used");
        Console.WriteLine("✅ Clean separation: abstractions vs implementations");
        Console.WriteLine("✅ Extensible: new databases add extension methods");
        Console.WriteLine("✅ Type-safe: compile-time checking through generics");
        Console.WriteLine("✅ Fluent API: discoverable and intuitive");
        Console.WriteLine("✅ Modular: each database is a separate NuGet package");
        Console.WriteLine();
        Console.WriteLine("🔧 Usage Pattern:");
        Console.WriteLine("1. Install provider NuGet packages");
        Console.WriteLine("2. Extension methods become available");
        Console.WriteLine("3. Configure with fluent API");
        Console.WriteLine("4. Services inject IDatabaseManager unchanged");
        Console.WriteLine();
        Console.WriteLine("🚀 Adding new database providers:");
        Console.WriteLine("- Create new library (e.g., FluentCMS.Database.PostgreSQL)");
        Console.WriteLine("- Implement IDatabaseManager");
        Console.WriteLine("- Add UsePostgreSQL() extension method");
        Console.WriteLine("- No changes needed to abstractions library!");
    }
}

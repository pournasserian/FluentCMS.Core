# FluentCMS Database Abstractions - Extensible Multi-Database Support

This library provides a completely extensible database abstraction system for FluentCMS with support for multiple databases and automatic assembly-based resolution.

## 🎯 Key Features

- **🚀 Extensible Architecture**: Add new database providers without modifying core abstractions
- **🔧 Fluent API**: Intuitive, discoverable configuration using method chaining
- **🏗️ Assembly-Based Resolution**: Automatically resolves the correct database based on calling assembly
- **📦 Modular Design**: Each database provider is a separate NuGet package
- **🔒 Type Safety**: Compile-time safety through generics and strong typing
- **⚡ Zero Breaking Changes**: Existing services continue to inject `IDatabaseManager` unchanged

## 🏛️ Architecture Overview

### Library Structure
```
FluentCMS.Database.Abstractions/           # Pure abstractions only
├── IDatabaseManager.cs                    # Core interface
├── IDatabaseMappingBuilder.cs             # Main fluent builder
├── IAssemblyMappingBuilder.cs             # Assembly-specific builder
├── DatabaseManagerResolver.cs             # Assembly resolution logic
└── ServiceCollectionExtensions.cs         # AddDatabaseManager()

FluentCMS.Database.SqlServer/               # SQL Server provider
├── SqlServerDatabaseManager.cs            # Implementation
└── SqlServerExtensions.cs                 # UseSqlServer() extension

FluentCMS.Database.Sqlite/                  # SQLite provider  
├── SqliteDatabaseManager.cs               # Implementation
└── SqliteExtensions.cs                    # UseSqlite() extension

FluentCMS.Database.PostgreSQL/              # Future: PostgreSQL provider
├── PostgreSqlDatabaseManager.cs           # Implementation
└── PostgreSqlExtensions.cs                # UsePostgreSQL() extension
```

## 🚀 Quick Start

### 1. Install Packages

Install the core abstractions and desired database providers:

```xml
<PackageReference Include="FluentCMS.Database.Abstractions" Version="1.0.0" />
<PackageReference Include="FluentCMS.Database.SqlServer" Version="1.0.0" />
<PackageReference Include="FluentCMS.Database.Sqlite" Version="1.0.0" />
```

### 2. Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FluentCMS;Trusted_Connection=true;",
    "UsersConnection": "Data Source=users.db",
    "ContentConnection": "Server=localhost;Database=FluentCMS_Content;Trusted_Connection=true;",
    "LogsConnection": "Data Source=logs.db"
  }
}
```

### 3. Register Services (Program.cs)

```csharp
using FluentCMS.Database.Abstractions;
using FluentCMS.Database.SqlServer;  // Enables UseSqlServer()
using FluentCMS.Database.Sqlite;     // Enables UseSqlite()

// Load connection strings from configuration
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
var usersConn = builder.Configuration.GetConnectionString("UsersConnection");
var contentConn = builder.Configuration.GetConnectionString("ContentConnection");

// Configure multi-database support with fluent API
builder.Services.AddDatabaseManager(options =>
{
    // Set default database for unmapped assemblies
    options.SetDefault().UseSqlServer(defaultConn);
    
    // Map specific assemblies to different databases
    options.MapAssembly<UserService>().UseSqlite(usersConn);
    options.MapAssembly<ContentService>().UseSqlServer(contentConn);
    
    // Future providers can be added without modifying this library:
    // options.MapAssembly<LoggingService>().UsePostgreSQL(logsConn);
    // options.MapAssembly<AnalyticsService>().UseMongoDB(analyticsConn);
});
```

### 4. Service Usage (No Changes Required!)

Services continue to inject `IDatabaseManager` as before - the resolution happens automatically:

```csharp
public class UserService
{
    private readonly IDatabaseManager _databaseManager;
    
    public UserService(IDatabaseManager databaseManager)
    {
        _databaseManager = databaseManager; // Automatically resolves to UsersConnection/Sqlite
    }
    
    public async Task<bool> UserTableExists(CancellationToken cancellationToken = default)
    {
        return await _databaseManager.TablesExist(new[] { "Users" }, cancellationToken);
    }
}

public class ContentService
{
    private readonly IDatabaseManager _databaseManager;
    
    public ContentService(IDatabaseManager databaseManager)
    {
        _databaseManager = databaseManager; // Automatically resolves to ContentConnection/SqlServer
    }
    
    public async Task InitializeContentDatabase(CancellationToken cancellationToken = default)
    {
        if (!await _databaseManager.DatabaseExists(cancellationToken))
        {
            await _databaseManager.CreateDatabase(cancellationToken);
        }
    }
}
```

## 🔧 How Assembly Resolution Works

1. When `IDatabaseManager` methods are called, the resolver analyzes the call stack
2. It identifies the calling assembly (e.g., assembly containing `UserService`)
3. It looks up the database configuration for that assembly
4. If no specific mapping exists, it uses the default configuration
5. It creates and returns the appropriate database manager using the registered factory

## 🛠️ Creating New Database Providers

Adding support for new databases is extremely simple and requires **zero changes** to the abstractions library:

### 1. Create New Provider Library

```csharp
// FluentCMS.Database.PostgreSQL/PostgreSqlDatabaseManager.cs
internal sealed class PostgreSqlDatabaseManager : IDatabaseManager
{
    // Implement IDatabaseManager interface
}

// FluentCMS.Database.PostgreSQL/PostgreSqlExtensions.cs
public static class PostgreSqlExtensions
{
    public static IAssemblyMappingBuilder UsePostgreSQL(this IAssemblyMappingBuilder builder, string connectionString)
    {
        return builder.RegisterProvider("PostgreSQL", connectionString, (connString, serviceProvider) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<PostgreSqlDatabaseManager>>();
            return new PostgreSqlDatabaseManager(connString, logger);
        });
    }
}
```

### 2. Install and Use

```xml
<PackageReference Include="FluentCMS.Database.PostgreSQL" Version="1.0.0" />
```

```csharp
using FluentCMS.Database.PostgreSQL; // Enables UsePostgreSQL()

services.AddDatabaseManager(options =>
{
    options.SetDefault().UseSqlServer(defaultConn);
    options.MapAssembly<AnalyticsService>().UsePostgreSQL(postgresConn);
});
```

## 🎯 Benefits Over Previous Architecture

| **Aspect** | **Old Architecture** | **New Architecture** |
|------------|---------------------|---------------------|
| **Database Types** | Hardcoded `DatabaseType` enum | Extensible string-based providers |
| **Adding Databases** | Modify abstractions library | Create separate extension library |
| **Provider Registration** | Manual registration required | Auto-registration via extension methods |
| **Configuration** | Enum + connection string name | Fluent API with direct connection strings |
| **Discoverability** | Hidden in enum values | IntelliSense shows available providers |
| **Modularity** | Monolithic library | Separate NuGet packages per database |
| **Breaking Changes** | Required for new databases | Never required |

## 🔒 Thread Safety & Performance

- **Thread-Safe**: The resolver supports concurrent access across multiple threads
- **Efficient**: Database manager instances are created per-request (scoped lifetime)
- **Cached**: Assembly resolution is optimized with intelligent call stack analysis
- **Lightweight**: Minimal overhead in the resolution process

## 📋 Database Operations

The `IDatabaseManager` interface provides these operations:

```csharp
// Check if database exists
await databaseManager.DatabaseExists(cancellationToken);

// Create database if it doesn't exist
await databaseManager.CreateDatabase(cancellationToken);

// Check if specific tables exist
await databaseManager.TablesExist(new[] { "Users", "Roles" }, cancellationToken);

// Check if tables are empty
await databaseManager.TablesEmpty(new[] { "Users" }, cancellationToken);

// Execute raw SQL
await databaseManager.ExecuteRaw("CREATE TABLE Users (Id int PRIMARY KEY)", cancellationToken);
```

## 🔄 Migration Guide

To migrate from the old hardcoded enum approach:

1. **Install new packages**: Reference the specific database provider packages you need
2. **Update registration**: Replace old registration with new fluent API
3. **Add using statements**: Import the provider namespaces for extension methods  
4. **No service changes**: Your existing services that inject `IDatabaseManager` work unchanged

## 🏗️ Future Extensibility Examples

The architecture supports any database technology:

```csharp
// Relational databases
options.MapAssembly<UserService>().UseMySQL(connString);
options.MapAssembly<ProductService>().UseOracle(connString);

// NoSQL databases  
options.MapAssembly<SessionService>().UseRedis(connString);
options.MapAssembly<DocumentService>().UseMongoDB(connString);
options.MapAssembly<GraphService>().UseNeo4j(connString);

// Cloud databases
options.MapAssembly<AnalyticsService>().UseCosmosDB(connString);
options.MapAssembly<LoggingService>().UseDynamoDB(connString);

// Time-series databases
options.MapAssembly<MetricsService>().UseInfluxDB(connString);
```

## 📚 Advanced Scenarios

### Assembly-Based Configuration

```csharp
// Map by assembly reference
var userAssembly = typeof(UserService).Assembly;
options.MapAssembly(userAssembly).UseSqlite(usersConn);

// Map by generic type
options.MapAssembly<ContentService>().UseSqlServer(contentConn);
```

### Error Handling

The system provides clear error messages for common issues:
- Missing default configuration
- Unknown assembly mappings
- Provider creation failures
- Connection string problems

This architecture provides a robust, extensible foundation for multi-database scenarios while maintaining backward compatibility and developer productivity.

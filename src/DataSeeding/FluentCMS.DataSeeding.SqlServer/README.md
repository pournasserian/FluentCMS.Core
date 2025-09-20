# FluentCMS.DataSeeding.SqlServer

SQL Server database support for the FluentCMS.DataSeeding library. This package provides SQL Server-specific implementations for database seeding operations with comprehensive features including transaction support, schema management, and production-safe configuration options.

## Features

- **SQL Server Integration**: Native support for Microsoft SQL Server databases
- **Transaction Support**: Configurable transaction isolation levels and scope
- **Schema Management**: Automatic schema creation and validation
- **Multiple Active Result Sets (MARS)**: Optional MARS support for complex operations
- **Production Safety**: Conditional execution and error handling strategies
- **Auto-Discovery**: Automatic registration of seeders and validators from assemblies
- **Priority-Based Execution**: Ordered execution of schema and data operations
- **Comprehensive Logging**: Detailed logging for debugging and monitoring

## Installation

```bash
dotnet add package FluentCMS.DataSeeding.SqlServer
```

## Quick Start

### Basic Configuration

```csharp
using FluentCMS.DataSeeding.SqlServer.Extensions;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add SQL Server data seeding
        builder.Services.AddSqlServerDataSeeding(
            "Server=localhost;Database=MyApp;Trusted_Connection=true");
        
        var app = builder.Build();
        app.Run();
    }
}
```

### Advanced Configuration

```csharp
builder.Services.AddSqlServerDataSeeding(
    connectionString, 
    options =>
    {
        // Configure assembly patterns for auto-discovery
        options.AssemblySearchPatterns.Add("MyApp.*.dll");
        options.AssemblySearchPatterns.Add("MyApp.Modules.*.dll");
        
        // Add development-only condition
        options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
        
        // Configure transaction behavior
        options.UseTransactions = true;
        options.IsolationLevel = IsolationLevel.ReadCommitted;
        
        // Configure error handling
        options.IgnoreExceptions = false; // Fail fast on errors
        
        // Configure schema settings
        options.DefaultSchema = "app";
        options.CreateDatabaseIfNotExists = false;
        
        // Enable SQL tracing for debugging
        options.EnableSqlTracing = true;
    });
```

## Configuration Options

### Convenience Methods

The package provides several convenience methods for common scenarios:

```csharp
// Development-only seeding
services.AddSqlServerDataSeedingForDevelopment(connectionString);

// Production-safe seeding with configuration-based conditions
services.AddSqlServerDataSeedingForProduction(
    connectionString, 
    "DataSeeding:Enabled");

// Custom schema configuration
services.AddSqlServerDataSeedingWithSchema(
    connectionString, 
    "tenant1");

// Transaction configuration
services.AddSqlServerDataSeedingWithTransactions(
    connectionString, 
    useTransactions: true, 
    IsolationLevel.Serializable);

// Minimal configuration with safety defaults
services.AddSqlServerDataSeedingMinimal(connectionString);
```

### SqlServerDataSeedingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | Required | SQL Server connection string |
| `AssemblySearchPatterns` | `List<string>` | Auto-detected | Patterns for discovering seeders |
| `Conditions` | `List<ICondition>` | Empty | Conditions that must pass for seeding |
| `IgnoreExceptions` | `bool` | `false` | Continue on individual seeder errors |
| `CreateDatabaseIfNotExists` | `bool` | `false` | Auto-create database if missing |
| `DefaultSchema` | `string` | `"dbo"` | Default schema for operations |
| `UseTransactions` | `bool` | `true` | Enable transaction support |
| `IsolationLevel` | `IsolationLevel` | `ReadCommitted` | Transaction isolation level |
| `EnableMars` | `bool` | `false` | Enable Multiple Active Result Sets |
| `CommandTimeoutSeconds` | `int` | `30` | Command timeout for operations |
| `EnableSqlTracing` | `bool` | `false` | Enable detailed SQL logging |

## Creating Seeders

### Schema Validator

```csharp
using FluentCMS.DataSeeding.Abstractions;
using FluentCMS.DataSeeding.Models;

public class UserTableValidator : ISchemaValidator
{
    public int Priority => 1;

    public async Task<bool> ValidateSchema(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var sqlContext = (SqlServerSeedingContext)context;
        return await sqlContext.TableExists("Users", cancellationToken: cancellationToken);
    }

    public async Task CreateSchema(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var sqlContext = (SqlServerSeedingContext)context;
        
        var createTableSql = @"
            CREATE TABLE [dbo].[Users] (
                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                [Email] NVARCHAR(255) NOT NULL UNIQUE,
                [Name] NVARCHAR(100) NOT NULL,
                [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            )";
            
        await sqlContext.ExecuteCommand(createTableSql, cancellationToken: cancellationToken);
    }
}
```

### Data Seeder

```csharp
public class AdminUserSeeder : IDataSeeder
{
    public int Priority => 10;

    public async Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var sqlContext = (SqlServerSeedingContext)context;
        var count = await sqlContext.GetRowCount("Users", whereClause: "Email = 'admin@example.com'", cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task SeedData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var sqlContext = (SqlServerSeedingContext)context;
        
        // Use transaction support
        await sqlContext.ExecuteInTransaction(async (connection, transaction, ct) =>
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO [dbo].[Users] ([Email], [Name]) 
                VALUES (@email, @name)";
            
            command.Parameters.AddWithValue("@email", "admin@example.com");
            command.Parameters.AddWithValue("@name", "System Administrator");
            
            await command.ExecuteNonQueryAsync(ct);
        }, cancellationToken);
    }
}
```

## Conditional Execution

### Built-in Conditions

```csharp
using FluentCMS.DataSeeding.Conditions;

// Environment-based conditions
options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
options.Conditions.Add(EnvironmentCondition.NotProduction());

// Configuration-based conditions
options.Conditions.Add(ConfigurationCondition.WhenEnabled("DataSeeding:Enabled"));
options.Conditions.Add(ConfigurationCondition.WhenValue("Environment:Type", "Staging"));

// Composite conditions
options.Conditions.Add(CompositeCondition.And(
    EnvironmentCondition.DevelopmentOnly(),
    ConfigurationCondition.WhenEnabled("Features:DataSeeding")
));
```

### Custom Conditions

```csharp
public class DatabaseEmptyCondition : ICondition
{
    public async Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var sqlContext = (SqlServerSeedingContext)context;
        
        // Check if any user tables exist with data
        var tableCount = await sqlContext.ExecuteScalar(@"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE' 
            AND TABLE_SCHEMA = 'dbo'", 
            cancellationToken: cancellationToken);
            
        return Convert.ToInt32(tableCount) == 0;
    }
}
```

## Error Handling

### Fail-Fast Strategy (Production)

```csharp
services.AddSqlServerDataSeeding(connectionString, options =>
{
    options.IgnoreExceptions = false; // Stop on first error
    options.UseTransactions = true;   // Rollback on failure
});
```

### Continue-on-Error Strategy (Development)

```csharp
services.AddSqlServerDataSeeding(connectionString, options =>
{
    options.IgnoreExceptions = true;  // Log errors but continue
    options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
});
```

## SQL Server Specific Features

### Schema Management

```csharp
// Create custom schema
await sqlContext.CreateSchemaIfNotExists("tenant1", cancellationToken);

// Check schema existence
var exists = await sqlContext.SchemaExists("tenant1", cancellationToken);
```

### Transaction Control

```csharp
// Execute with custom transaction
await sqlContext.ExecuteInTransaction(async (connection, transaction, ct) =>
{
    // Multiple operations within single transaction
    await ExecuteMultipleCommands(connection, transaction, ct);
}, cancellationToken);
```

### Connection Management

```csharp
// Long-running operations
using var connection = await sqlContext.CreateAndOpenConnection(cancellationToken);
// Use connection for multiple operations
```

## Production Deployment

### Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=MyApp;Trusted_Connection=true"
  },
  "DataSeeding": {
    "Enabled": false,
    "IgnoreErrors": false
  }
}
```

### Conditional Registration

```csharp
var enableSeeding = builder.Configuration.GetValue<bool>("DataSeeding:Enabled");

if (enableSeeding)
{
    builder.Services.AddSqlServerDataSeedingForProduction(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        "DataSeeding:Enabled");
}
```

## Monitoring and Debugging

### Logging Configuration

```csharp
builder.Logging.AddFilter("FluentCMS.DataSeeding.SqlServer", LogLevel.Debug);
```

### Seeding Statistics

```csharp
public class SeedingStatsService
{
    private readonly SqlServerDataSeedingEngine _engine;
    
    public async Task<Dictionary<string, object>> GetStatistics()
    {
        return await _engine.GetSeedingStatistics();
    }
}
```

## Best Practices

1. **Use Priority Ordering**: Ensure schema validators run before data seeders
2. **Implement Idempotent Seeders**: Check for existing data before inserting
3. **Use Transactions**: Enable transactions for data consistency
4. **Environment Conditions**: Restrict seeding to appropriate environments
5. **Error Handling**: Choose appropriate strategy based on environment
6. **Schema Organization**: Use custom schemas for multi-tenant applications
7. **Connection Security**: Use integrated security or secure credential storage

## Integration Examples

### With Entity Framework Core

```csharp
public class EfCoreUserSeeder : IDataSeeder
{
    private readonly UserDbContext _context;
    
    public EfCoreUserSeeder(UserDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(cancellationToken);
    }
    
    public async Task SeedData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        _context.Users.AddRange(GetSeedUsers());
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### With Dapper

```csharp
public class DapperProductSeeder : IDataSeeder
{
    public async Task SeedData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var sqlContext = (SqlServerSeedingContext)context;
        
        using var connection = await sqlContext.CreateAndOpenConnection(cancellationToken);
        
        var products = GetSeedProducts();
        await connection.ExecuteAsync(@"
            INSERT INTO Products (Name, Price, CategoryId) 
            VALUES (@Name, @Price, @CategoryId)", 
            products);
    }
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

# FluentCMS.DataSeeding - Project Brief & Specification

## Project Overview

FluentCMS.DataSeeding is a flexible and extensible database seeding library for ASP.NET Core applications. It provides database-agnostic seeding capabilities with auto-discovery of schema validators and data seeders, supporting conditional execution and priority-based ordering.

### Key Features

- **Database Agnostic**: Extensible architecture supporting multiple database engines
- **Auto-Discovery**: Automatic discovery and registration of seeders and validators
- **Priority-Based Execution**: Simple integer-based ordering system
- **Conditional Seeding**: Environment and configuration-driven execution
- **Hosted Service Integration**: Automatic execution at application startup
- **Custom Schema Validation**: Developer-defined schema validation and creation logic
- **Flexible Configuration**: Minimal startup configuration with convention over configuration approach

## Architecture Design

### Project Structure

```
FluentCMS.DataSeeding/
├── Abstractions/
│   ├── ISchemaValidator.cs
│   ├── IDataSeeder.cs
│   └── ICondition.cs
├── Models/
│   ├── SeedingContext.cs
│   └── SeedingResult.cs
├── Conditions/
│   ├── EnvironmentCondition.cs
│   ├── ConfigurationCondition.cs
│   ├── DataStateCondition.cs
│   └── CompositeCondition.cs
└── Engine/
    ├── AssemblyScanner.cs
    └── DependencyResolver.cs

FluentCMS.DataSeeding.Sqlite/
├── SqliteDataSeedingOptions.cs
├── DataSeedingHostedService.cs
├── SqliteSeedingContext.cs
├── SqliteDataSeedingEngine.cs
└── ServiceCollectionExtensions.cs
```

### Core Namespaces

- **FluentCMS.DataSeeding**: Main library containing abstractions, conditions, and engine
- **FluentCMS.DataSeeding.Sqlite**: SQLite-specific implementation (initial target)
- **FluentCMS.DataSeeding.SqlServer**: Future SQL Server implementation
- **FluentCMS.DataSeeding.MySQL**: Future MySQL implementation
- **FluentCMS.DataSeeding.MongoDB**: Future MongoDB implementation

### Public vs Internal Architecture

The library maintains a clear separation between public API and internal implementation:

**Public API Surface (what consumers use):**
- All interface definitions (`ISchemaValidator`, `IDataSeeder`, `ICondition`)
- Built-in condition implementations (`EnvironmentCondition`, `ConfigurationCondition`, etc.)
- Configuration classes (`SqliteDataSeedingOptions`)
- Extension methods (`AddSqliteDataSeeding()`)
- Public models (`SeedingContext`, `SeedingResult`)

**Internal Implementation (hidden from consumers):**
- `AssemblyScanner` - handles pattern-based assembly discovery
- `DependencyResolver` - manages priority-based execution ordering
- `DataSeedingHostedService` - background service orchestration
- `SqliteSeedingContext` - database-specific context implementation
- `SqliteDataSeedingEngine` - core seeding execution logic

This design ensures a clean, minimal public API while keeping implementation details internal and subject to change without breaking consumer code.

## Interface Specifications

### ISchemaValidator

```csharp
/// <summary>
/// Validates and creates database schema as needed.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Execution priority. Lower numbers execute first.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Validates if the database schema exists and is correct.
    /// </summary>
    /// <param name="context">Seeding context containing connection and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if schema is valid, false if schema needs to be created</returns>
    Task<bool> ValidateSchema(SeedingContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates or updates the database schema.
    /// </summary>
    /// <param name="context">Seeding context containing connection and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CreateSchema(SeedingContext context, CancellationToken cancellationToken = default);
}
```

### IDataSeeder

```csharp
/// <summary>
/// Seeds data into database tables or collections.
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Execution priority. Lower numbers execute first.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Checks if data already exists in the target table/collection.
    /// </summary>
    /// <param name="context">Seeding context containing connection and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if data exists, false if seeding is needed</returns>
    Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Seeds the data into the database.
    /// </summary>
    /// <param name="context">Seeding context containing connection and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedData(SeedingContext context, CancellationToken cancellationToken = default);
}
```

### ICondition

```csharp
/// <summary>
/// Defines conditions for when seeding should execute.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// Evaluates whether seeding should proceed.
    /// </summary>
    /// <param name="context">Seeding context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if seeding should proceed, false otherwise</returns>
    Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default);
}
```

## Configuration API

### Startup Configuration

```csharp
// Program.cs or Startup.cs
services.AddSqliteDataSeeding(connectionString, options =>
{
    // Assembly scanning patterns - supports wildcards for flexible discovery
    options.AssemblySearchPatterns.Add("FluentCMS.*.dll");
    options.AssemblySearchPatterns.Add("FluentCMS.*.Data.*.dll");
    options.AssemblySearchPatterns.Add("MyApp.Modules.*.dll");
    
    // Global conditions - all must pass for seeding to occur
    options.Conditions.Add(new EnvironmentCondition(
        builder.Environment,
        env => env.IsDevelopment()));
        
    options.Conditions.Add(new ConfigurationCondition(
        builder.Configuration,
        config => config.GetValue<bool>("EnableSeeding")));
    
    // Exception handling strategy
    options.IgnoreExceptions = false; // Stop on first exception (default)
    // options.IgnoreExceptions = true; // Continue seeding even if some fail
});
```

### SqliteDataSeedingOptions

```csharp
public class SqliteDataSeedingOptions
{
    /// <summary>
    /// Assembly search patterns to scan for seeders and validators.
    /// Supports wildcard patterns for flexible assembly discovery.
    /// Examples: 
    /// - "FluentCMS.*.dll" - matches FluentCMS.Core.dll, FluentCMS.Accounting.dll
    /// - "FluentCMS.*.Data.*.dll" - matches FluentCMS.Core.Data.Seeding.dll, FluentCMS.Modules.Data.Setup.dll
    /// - "MyApp.Modules.*.dll" - matches MyApp.Modules.Users.dll, MyApp.Modules.Products.dll
    /// </summary>
    public List<string> AssemblySearchPatterns { get; } = new();
    
    /// <summary>
    /// Global conditions that must all pass for seeding to execute.
    /// </summary>
    public List<ICondition> Conditions { get; } = new();
    
    /// <summary>
    /// Whether to continue seeding if an exception occurs.
    /// False (default): Stop execution on first exception
    /// True: Log exception and continue with next seeder
    /// </summary>
    public bool IgnoreExceptions { get; set; } = false;
    
    /// <summary>
    /// SQLite connection string.
    /// </summary>
    public string ConnectionString { get; set; }
}
```

## Implementation Examples

### Schema Validator Example

```csharp
public class CoreSchemaValidator : ISchemaValidator
{
    public int Priority => 10; // Execute first
    
    public async Task<bool> ValidateSchema(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Check if core tables exist
        using var connection = context.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) FROM sqlite_master 
            WHERE type='table' AND name IN ('Users', 'Roles', 'UserRoles')";
        
        var count = (long)await command.ExecuteScalarAsync(cancellationToken);
        return count == 3; // All core tables exist
    }
    
    public async Task CreateSchema(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Create core database schema
        using var connection = context.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Email TEXT NOT NULL UNIQUE,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );
            
            CREATE TABLE IF NOT EXISTS Roles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );
            
            CREATE TABLE IF NOT EXISTS UserRoles (
                UserId INTEGER,
                RoleId INTEGER,
                PRIMARY KEY (UserId, RoleId),
                FOREIGN KEY (UserId) REFERENCES Users(Id),
                FOREIGN KEY (RoleId) REFERENCES Roles(Id)
            );";
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
```

### Data Seeder Example

```csharp
public class RoleSeeder : IDataSeeder
{
    public int Priority => 10; // Execute before user seeder
    
    public async Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Check if roles already exist
        using var connection = context.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Roles";
        
        var count = (long)await command.ExecuteScalarAsync(cancellationToken);
        return count > 0;
    }
    
    public async Task SeedData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Seed default roles
        using var connection = context.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Roles (Name) VALUES 
            ('Administrator'),
            ('Editor'),
            ('Viewer')";
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public class UserSeeder : IDataSeeder
{
    public int Priority => 20; // Execute after roles are seeded
    
    public async Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Check if users already exist
        using var connection = context.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users";
        
        var count = (long)await command.ExecuteScalarAsync(cancellationToken);
        return count > 0;
    }
    
    public async Task SeedData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Seed default admin user
        using var connection = context.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Users (Username, Email) VALUES 
            ('admin', 'admin@fluentcms.com');
            
            INSERT INTO UserRoles (UserId, RoleId) 
            SELECT u.Id, r.Id FROM Users u, Roles r 
            WHERE u.Username = 'admin' AND r.Name = 'Administrator'";
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
```

### Custom Condition Example

```csharp
public class EnvironmentCondition : ICondition
{
    private readonly IWebHostEnvironment _environment;
    private readonly Func<IWebHostEnvironment, bool> _predicate;
    
    public EnvironmentCondition(IWebHostEnvironment environment, Func<IWebHostEnvironment, bool> predicate)
    {
        _environment = environment;
        _predicate = predicate;
    }
    
    public Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_predicate(_environment));
    }
}

public class ConfigurationCondition : ICondition
{
    private readonly IConfiguration _configuration;
    private readonly Func<IConfiguration, bool> _predicate;
    
    public ConfigurationCondition(IConfiguration configuration, Func<IConfiguration, bool> predicate)
    {
        _configuration = configuration;
        _predicate = predicate;
    }
    
    public Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_predicate(_configuration));
    }
}
```

## Execution Flow

The seeding process follows this sequence:

1. **Service Registration**: `AddSqliteDataSeeding()` scans assemblies and auto-registers all discovered implementations
2. **Condition Evaluation**: All global conditions must pass for seeding to proceed
3. **Schema Validation**: Execute all schema validators in priority order
4. **Schema Creation**: If any validator fails, execute corresponding schema creation
5. **Data Seeding**: Execute all data seeders in priority order
6. **Exception Handling**: Based on `IgnoreExceptions` setting, either stop or continue on errors

## Auto-Discovery Mechanism

### Assembly Scanning Process

1. **Pattern Matching**: Find assemblies matching configured search patterns with wildcard support
2. **Type Discovery**: Scan for classes implementing `ISchemaValidator` and `IDataSeeder`
3. **Validation**: Ensure implementations are concrete classes with public constructors
4. **Registration**: Automatically register all discovered types in DI container
5. **Ordering**: Sort by Priority property for execution order

### Example Discovery Configuration

```csharp
// Simple wildcard patterns
options.AssemblySearchPatterns.Add("FluentCMS.*.dll");           // Matches: FluentCMS.Core.dll, FluentCMS.Accounting.dll

// Complex nested patterns
options.AssemblySearchPatterns.Add("FluentCMS.*.Data.*.dll");    // Matches: FluentCMS.Core.Data.Seeding.dll, FluentCMS.Modules.Data.Setup.dll

// Module-specific patterns
options.AssemblySearchPatterns.Add("MyApp.Modules.*.dll");       // Matches: MyApp.Modules.Users.dll, MyApp.Modules.Products.dll

// Multiple pattern support for different naming conventions
options.AssemblySearchPatterns.Add("*.Seeding.dll");             // Matches: Any.Assembly.Seeding.dll
options.AssemblySearchPatterns.Add("*.DataSeeding.dll");         // Matches: Any.Assembly.DataSeeding.dll
```

### Pattern Matching Rules

- **Single Wildcard (*)**: Matches any sequence of characters within a single segment
- **Multiple Wildcards**: Supported for complex nested structures
- **Case Sensitivity**: Pattern matching is case-insensitive on Windows, case-sensitive on Unix systems
- **Extension Handling**: Always include ".dll" extension in patterns for clarity

## Technical Requirements

### Framework Support
- **.NET 9+** 
- **ASP.NET Core 9+** for hosted service integration

### Dependencies
- **Microsoft.Data.Sqlite**: SQLite database connectivity
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **Microsoft.Extensions.Hosting**: Background service support
- **Microsoft.Extensions.Configuration**: Configuration binding

### Performance Considerations
- **Lazy Loading**: Seeders are only instantiated when needed
- **Connection Management**: Proper disposal of database connections
- **Memory Efficiency**: Minimal memory footprint during execution

## Development Guidelines

### Coding Standards
- **Async/Await**: All database operations must be asynchronous
- **CancellationToken**: Pass cancellation tokens to all async methods with default value
- **Inline Comments**: Write inline comments for complex logic
- **No XML Documentation**: Use inline comments instead of XML documentation

### Error Handling
- **Exception Strategy**: Configurable via `IgnoreExceptions` option
- **Detailed Logging**: Log all seeding operations and errors
- **Context Information**: Include seeder type and priority in error messages

## Usage Scenarios

### Basic Usage

```csharp
// Minimal configuration - development environment only
services.AddSqliteDataSeeding("Data Source=app.db", options =>
{
    options.AssemblySearchPatterns.Add("MyApp.*.dll");
    options.Conditions.Add(new EnvironmentCondition(env, env => env.IsDevelopment()));
});
```

### Advanced Configuration

```csharp
// Production-ready configuration with multiple conditions
services.AddSqliteDataSeeding(connectionString, options =>
{
    // Scan multiple assembly patterns with flexible wildcards
    options.AssemblySearchPatterns.Add("FluentCMS.*.dll");
    options.AssemblySearchPatterns.Add("FluentCMS.*.Data.*.dll");
    options.AssemblySearchPatterns.Add("MyApp.Core.*.dll");
    options.AssemblySearchPatterns.Add("MyApp.Modules.*.dll");
    
    // Multiple conditions with composite logic
    options.Conditions.Add(new EnvironmentCondition(env, env => !env.IsProduction()));
    options.Conditions.Add(new ConfigurationCondition(config, c => c.GetValue<bool>("Database:EnableSeeding")));
    
    // Continue on errors for resilient seeding
    options.IgnoreExceptions = true;
});
```

## Extension Points

### Database Engine Support
The architecture supports additional database engines by implementing:
- Database-specific `SeedingContext`
- Database-specific service registration extensions
- Database-specific hosted service implementation

### Custom Conditions
Developers can create custom conditions by implementing `ICondition`:

```csharp
public class TimeBasedCondition : ICondition
{
    public Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Only seed during business hours
        var hour = DateTime.Now.Hour;
        return Task.FromResult(hour >= 9 && hour <= 17);
    }
}
```

## Future Enhancements

1. **Additional Database Engines**: SQL Server, MySQL, PostgreSQL, MongoDB
2. **Data Import**: Support for CSV, JSON, XML data sources
3. **Performance Metrics**: Execution time tracking and reporting

## Summary

FluentCMS.DataSeeding provides a comprehensive, developer-friendly solution for database seeding in ASP.NET Core applications. Its auto-discovery mechanism, priority-based execution, and conditional seeding capabilities make it suitable for both simple and complex scenarios while maintaining clean, maintainable code.

# TestSeeder - Generic Database Seeding Library for ASP.NET Core

A flexible and extensible database seeding library for ASP.NET Core applications using Entity Framework Core. Supports SQL Server and SQLite with configurable conditions for when seeding should occur.

## Features

- **Multi-Database Support**: Works with SQL Server and SQLite
- **Conditional Seeding**: Define custom conditions for when seeding should occur
- **Code-Based Seeders**: Implement seeders using C# classes
- **Automatic Discovery**: Automatically discovers and registers seeder classes
- **Transaction Support**: Optional transaction wrapping for data consistency
- **Retry Logic**: Built-in retry mechanism for failed seeding operations
- **Logging Integration**: Comprehensive logging support
- **Easy Integration**: Simple DI registration and middleware integration

## Installation

Add the TestSeeder library to your ASP.NET Core project.

## Quick Start

### 1. Create a Seeder Class

```csharp
using TestSeeder.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

public class UserSeeder : ISeeder<User>
{
    public int Order => 1; // Execution order

    public async Task Seed(DbContext context)
    {
        if (!context.Set<User>().Any())
        {
            var users = new[]
            {
                new User { Name = "John Doe", Email = "john@example.com" },
                new User { Name = "Jane Smith", Email = "jane@example.com" }
            };

            context.Set<User>().AddRange(users);
            await context.SaveChangesAsync();
        }
    }
}
```

### 2. Register Services in Program.cs

```csharp
using TestSeeder.Extensions;
using TestSeeder.Conditions;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add database seeding
builder.Services.AddDatabaseSeeding<MyDbContext>(options =>
{
    options.EnsureSchemaCreated = true;
    options.UseTransaction = true;
    options.EnableLogging = true;
});

var app = builder.Build();

// Use database seeding middleware
app.UseDatabaseSeeding<MyDbContext>();

app.Run();
```

## Advanced Configuration

### Conditional Seeding

Define conditions that must be met before seeding occurs:

```csharp
builder.Services.AddDatabaseSeeding<MyDbContext>(options =>
{
    // Only seed in Development environment
    options.Conditions.Add(new EnvironmentCondition(
        builder.Environment, 
        env => env.IsDevelopment()));
    
    // Only seed if specific tables are empty
    options.Conditions.Add(new TablesEmptyCondition("Users", "Products"));
    
    // Only seed if configuration setting is enabled
    options.Conditions.Add(new ConfigurationCondition(
        builder.Configuration, 
        "EnableSeeding", 
        "true"));
});
```

### Multiple Database Support

```csharp
// SQL Server
builder.Services.AddDbContext<SqlServerContext>(options =>
    options.UseSqlServer(sqlServerConnectionString));

// SQLite
builder.Services.AddDbContext<SqliteContext>(options =>
    options.UseSqlite(sqliteConnectionString));

// Register seeding for both
builder.Services.AddDatabaseSeeding<SqlServerContext>();
builder.Services.AddDatabaseSeeding<SqliteContext>();

// Use seeding for both contexts
app.UseDatabaseSeeding(typeof(SqlServerContext), typeof(SqliteContext));
```

### Custom Conditions

Create custom seeding conditions:

```csharp
public class CustomCondition : ISeedingCondition
{
    public string Name => "Custom Business Logic";

    public async Task<bool> ShouldSeed(DbContext context)
    {
        // Your custom logic here
        return await SomeBusinessLogicCheck();
    }
}

// Register the custom condition
builder.Services.AddDatabaseSeeding<MyDbContext>(options =>
{
    options.Conditions.Add(new CustomCondition());
});
```

### Composite Conditions

Combine multiple conditions with AND/OR logic:

```csharp
var condition1 = new EnvironmentCondition(environment, env => env.IsDevelopment());
var condition2 = new TablesEmptyCondition("Users");

// Both conditions must be true (AND logic)
var andCondition = new CompositeCondition(true, condition1, condition2);

// At least one condition must be true (OR logic)
var orCondition = new CompositeCondition(false, condition1, condition2);

builder.Services.AddDatabaseSeeding<MyDbContext>(options =>
{
    options.Conditions.Add(andCondition);
});
```

## Configuration Options

```csharp
builder.Services.AddDatabaseSeeding<MyDbContext>(options =>
{
    // Whether to create database schema if it doesn't exist
    options.EnsureSchemaCreated = true;
    
    // Whether to run seeders in a transaction
    options.UseTransaction = true;
    
    // Maximum retry attempts for failed operations
    options.MaxRetryAttempts = 3;
    
    // Delay between retry attempts (milliseconds)
    options.RetryDelayMs = 1000;
    
    // Whether to enable logging
    options.EnableLogging = true;
    
    // Assemblies to scan for seeder implementations
    options.AssembliesToScan.Add("MyProject.Seeders.dll");
});
```

## Manual Seeding

Execute seeding manually without middleware:

```csharp
public class SeedController : ControllerBase
{
    private readonly ISeedingService _seedingService;
    private readonly MyDbContext _context;

    public SeedController(ISeedingService seedingService, MyDbContext context)
    {
        _seedingService = seedingService;
        _context = context;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        await _seedingService.ExecuteSeeding(_context);
        return Ok("Seeding completed");
    }
}
```

## Built-in Conditions

### DatabaseExistsCondition
Checks if the database exists and can be connected to.

### TablesEmptyCondition
Checks if specified tables are empty.

### EnvironmentCondition
Checks the current hosting environment (Development, Production, etc.).

### ConfigurationCondition
Checks configuration settings for specific values.

### CompositeCondition
Combines multiple conditions with AND/OR logic.

## Best Practices

1. **Order Your Seeders**: Use the `Order` property to control execution sequence
2. **Check Before Seeding**: Always check if data already exists before adding new records
3. **Use Transactions**: Enable transactions for data consistency
4. **Environment-Specific Seeding**: Use conditions to control seeding per environment
5. **Logging**: Enable logging to monitor seeding operations
6. **Error Handling**: Implement proper error handling in your seeders

## Example Seeder with Relationships

```csharp
public class ProductSeeder : ISeeder<Product>
{
    public int Order => 2; // Run after UserSeeder

    public async Task Seed(DbContext context)
    {
        if (!context.Set<Product>().Any())
        {
            // Ensure users exist first
            var user = await context.Set<User>().FirstAsync();
            
            var products = new[]
            {
                new Product 
                { 
                    Name = "Laptop", 
                    Price = 999.99m, 
                    CreatedBy = user.Id 
                },
                new Product 
                { 
                    Name = "Mouse", 
                    Price = 29.99m, 
                    CreatedBy = user.Id 
                }
            };

            context.Set<Product>().AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}
```

## License

This library is provided as-is for educational and development purposes.

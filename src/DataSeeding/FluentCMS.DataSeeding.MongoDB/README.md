# FluentCMS.DataSeeding.MongoDB

MongoDB implementation for the FluentCMS.DataSeeding library, providing database seeding capabilities for MongoDB databases with auto-discovery, priority-based execution, and conditional seeding.

## Installation

Add the MongoDB data seeding package to your project:

```bash
dotnet add package FluentCMS.DataSeeding.MongoDB
```

## Quick Start

### Basic Setup

```csharp
// Program.cs
using FluentCMS.DataSeeding.MongoDB.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add MongoDB data seeding
builder.Services.AddMongoDbDataSeeding(
    "mongodb://localhost:27017", 
    "MyAppDatabase"
);

var app = builder.Build();
app.Run();
```

### Development-Only Seeding

```csharp
// Only run seeding in Development environment
builder.Services.AddMongoDbDataSeedingForDevelopment(
    "mongodb://localhost:27017", 
    "MyAppDatabase", 
    options =>
    {
        options.AssemblySearchPatterns.Add("MyApp.*.dll");
        options.IgnoreExceptions = true;
    }
);
```

## Creating Schema Validators

Schema validators create and validate MongoDB collections and indexes:

```csharp
using FluentCMS.DataSeeding.Abstractions;
using FluentCMS.DataSeeding.Models;
using FluentCMS.DataSeeding.MongoDB.Context;

public class UserCollectionValidator : ISchemaValidator
{
    public int Priority => 10; // Schema validators: 1-99

    public async Task<bool> ValidateSchema(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var mongoContext = (MongoDbSeedingContext)context;
        return await mongoContext.CollectionExists("Users");
    }

    public async Task CreateSchema(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var mongoContext = (MongoDbSeedingContext)context;
        
        // Create collection
        await mongoContext.CreateCollection("Users");
        
        // Create indexes
        await mongoContext.CreateIndex("Users", "Email", unique: true);
        await mongoContext.CreateCompoundIndex("Users", new Dictionary<string, bool>
        {
            ["LastName"] = true,  // ascending
            ["FirstName"] = true  // ascending
        });
    }
}
```

## Creating Data Seeders

Data seeders insert initial or test data into collections:

```csharp
using FluentCMS.DataSeeding.Abstractions;
using FluentCMS.DataSeeding.Models;
using FluentCMS.DataSeeding.MongoDB.Context;
using MongoDB.Bson;

public class AdminUserSeeder : IDataSeeder
{
    public int Priority => 100; // Data seeders: 100+

    public async Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var mongoContext = (MongoDbSeedingContext)context;
        var userCount = await mongoContext.GetDocumentCount("Users");
        return userCount > 0;
    }

    public async Task SeedData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var mongoContext = (MongoDbSeedingContext)context;
        var collection = mongoContext.GetCollection<BsonDocument>("Users");

        var adminUser = new BsonDocument
        {
            ["_id"] = ObjectId.GenerateNewId(),
            ["Email"] = "admin@myapp.com",
            ["FirstName"] = "Admin",
            ["LastName"] = "User",
            ["IsActive"] = true,
            ["CreatedAt"] = DateTime.UtcNow
        };

        await collection.InsertOneAsync(adminUser, cancellationToken: cancellationToken);
    }
}
```

## Working with Typed Documents

```csharp
// Define your document model
public class User
{
    public ObjectId Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Use in seeder
public class TypedUserSeeder : IDataSeeder
{
    public int Priority => 110;

    public async Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var mongoContext = (MongoDbSeedingContext)context;
        var count = await mongoContext.GetDocumentCount<User>("Users");
        return count > 1; // Admin user already exists
    }

    public async Task SeedData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var mongoContext = (MongoDbSeedingContext)context;
        var collection = mongoContext.GetCollection<User>("Users");

        var testUsers = new[]
        {
            new User
            {
                Email = "john.doe@example.com",
                FirstName = "John",
                LastName = "Doe",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Email = "jane.smith@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await collection.InsertManyAsync(testUsers, cancellationToken: cancellationToken);
    }
}
```

## Configuration Options

### Complete Configuration Example

```csharp
builder.Services.AddMongoDbDataSeeding(
    "mongodb://localhost:27017", 
    "MyAppDatabase", 
    options =>
    {
        // Assembly discovery patterns
        options.AssemblySearchPatterns.Add("MyApp.Core.dll");
        options.AssemblySearchPatterns.Add("MyApp.Data.dll");
        options.AssemblySearchPatterns.Add("MyApp.*.dll");

        // Conditional execution
        options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
        options.Conditions.Add(ConfigurationCondition.IsTrue("EnableDataSeeding"));

        // Error handling
        options.IgnoreExceptions = false; // Fail fast on errors

        // MongoDB-specific options
        options.OperationTimeoutSeconds = 30;
        options.MaxConnectionPoolSize = 100;
        options.MinConnectionPoolSize = 5;
        options.UseSsl = false;
        options.CreateIndexes = true;
        
        // Dangerous: drops all collections before seeding
        options.DropCollectionsBeforeSeeding = false;
    }
);
```

### Convenience Methods

```csharp
// Local development with defaults
builder.Services.AddMongoDbDataSeedingForLocalDevelopment("MyAppDatabase");

// With error continuation
builder.Services.AddMongoDbDataSeedingWithErrorContinuation(
    "mongodb://localhost:27017", 
    "MyAppDatabase"
);

// Minimal configuration
builder.Services.AddMongoDbDataSeedingMinimal(
    "mongodb://localhost:27017", 
    "MyAppDatabase"
);

// For staging environment
builder.Services.AddMongoDbDataSeedingForStaging(
    connectionString, 
    "MyAppDatabase"
);

// With collection dropping (DANGEROUS!)
builder.Services.AddMongoDbDataSeedingWithCollectionDrop(
    "mongodb://localhost:27017", 
    "MyAppDatabase"
);
```

## Conditional Seeding

Control when seeding executes using built-in conditions:

```csharp
builder.Services.AddMongoDbDataSeeding(connectionString, databaseName, options =>
{
    // Only in Development environment
    options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
    
    // Only when configuration setting is true
    options.Conditions.Add(ConfigurationCondition.IsTrue("EnableSeeding"));
    
    // Complex condition combinations
    options.Conditions.Add(CompositeCondition.All(
        EnvironmentCondition.NotProduction(),
        ConfigurationCondition.Exists("ConnectionStrings:MongoDB")
    ));
});
```

## Priority-Based Execution

Components execute in priority order:

- **Schema Validators**: 1-99 (lower numbers first)
- **Data Seeders**: 100+ (lower numbers first)

```csharp
public class DatabaseValidator : ISchemaValidator
{
    public int Priority => 1; // Runs first
}

public class UserCollectionValidator : ISchemaValidator
{
    public int Priority => 10; // Runs after database setup
}

public class AdminSeeder : IDataSeeder
{
    public int Priority => 100; // First data seeder
}

public class TestDataSeeder : IDataSeeder
{
    public int Priority => 200; // Runs after admin data
}
```

## MongoDB-Specific Features

### Collection Operations

```csharp
var mongoContext = (MongoDbSeedingContext)context;

// Check if collection exists
bool exists = await mongoContext.CollectionExists("Users");

// Get document count
long count = await mongoContext.GetDocumentCount("Users");

// Get typed document count with filter
var activeUserCount = await mongoContext.GetDocumentCount<User>("Users", 
    Builders<User>.Filter.Eq(u => u.IsActive, true));

// Create collection with options
await mongoContext.CreateCollection("Users", new CreateCollectionOptions
{
    Capped = false,
    MaxDocuments = 1000000
});

// Drop collection
await mongoContext.DropCollection("OldCollection");
```

### Index Management

```csharp
// Simple index
await mongoContext.CreateIndex("Users", "Email", unique: true);

// Compound index
await mongoContext.CreateCompoundIndex("Users", new Dictionary<string, bool>
{
    ["LastName"] = true,   // ascending
    ["FirstName"] = true,  // ascending
    ["CreatedAt"] = false  // descending
}, unique: false);
```

### Working with Collections

```csharp
// Get typed collection
var userCollection = mongoContext.GetCollection<User>("Users");

// Get BsonDocument collection
var documentCollection = mongoContext.GetCollection<BsonDocument>("Users");

// Access database directly
var database = mongoContext.GetDatabase();
var collections = await database.ListCollectionNamesAsync();
```

## Error Handling

Configure how seeding handles errors:

```csharp
builder.Services.AddMongoDbDataSeeding(connectionString, databaseName, options =>
{
    // Fail fast (default) - stop on first error
    options.IgnoreExceptions = false;
    
    // Continue on errors - log and continue with next component
    options.IgnoreExceptions = true;
});
```

## Assembly Discovery

Control which assemblies are scanned for seeders and validators:

```csharp
options.AssemblySearchPatterns.Clear();
options.AssemblySearchPatterns.Add("MyApp.dll");           // Specific assembly
options.AssemblySearchPatterns.Add("MyApp.*.dll");         // Wildcard pattern
options.AssemblySearchPatterns.Add("MyCompany.*.dll");     // Company assemblies
```

## Logging

The library provides comprehensive logging:

```json
{
  "Logging": {
    "LogLevel": {
      "FluentCMS.DataSeeding.MongoDB": "Information"
    }
  }
}
```

Log levels:
- **Information**: Seeding start/completion, statistics
- **Debug**: Component discovery, execution flow  
- **Trace**: Individual component creation and execution

## Connection Strings

Support for various MongoDB connection string formats:

```csharp
// Simple local connection
"mongodb://localhost:27017"

// With authentication
"mongodb://username:password@localhost:27017"

// Replica set
"mongodb://mongo1:27017,mongo2:27017,mongo3:27017/?replicaSet=myReplicaSet"

// With SSL and authentication database
"mongodb://user:pass@cluster.mongodb.net:27017/?ssl=true&authSource=admin"

// Full connection string with options
"mongodb://user:pass@localhost:27017/mydb?authSource=admin&ssl=true&connectTimeoutMS=30000"
```

## Best Practices

### 1. Environment Safety
Always use environment conditions for production safety:

```csharp
options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
```

### 2. Priority Planning
Leave gaps between priorities for future insertion:

```csharp
public int Priority => 10;  // Not 1, 2, 3...
public int Priority => 20;  // Leave room for insertion
public int Priority => 30;
```

### 3. Idempotent Operations
Always check for existing data before seeding:

```csharp
public async Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default)
{
    var mongoContext = (MongoDbSeedingContext)context;
    return await mongoContext.GetDocumentCount("Users") > 0;
}
```

### 4. Proper Error Handling
Handle MongoDB-specific exceptions:

```csharp
try
{
    await collection.InsertOneAsync(document);
}
catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
{
    // Handle duplicate key error
    _logger.LogWarning("Document already exists: {Message}", ex.Message);
}
```

### 5. Index Strategy
Create indexes in schema validators, not data seeders:

```csharp
// ✅ Good - in schema validator
public async Task CreateSchema(SeedingContext context, CancellationToken cancellationToken = default)
{
    await mongoContext.CreateIndex("Users", "Email", unique: true);
    // Then create collection
}

// ❌ Bad - don't create indexes in data seeders
```

## Advanced Usage

### Custom Conditions

```csharp
public class TimeBasedCondition : ICondition
{
    public Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var hour = DateTime.Now.Hour;
        return Task.FromResult(hour >= 9 && hour <= 17); // Business hours only
    }
}

// Use in configuration
options.Conditions.Add(new TimeBasedCondition());
```

### Dependency Injection in Seeders

```csharp
public class UserSeeder : IDataSeeder
{
    private readonly IUserService _userService;
    private readonly ILogger<UserSeeder> _logger;

    public UserSeeder(IUserService userService, ILogger<UserSeeder> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public int Priority => 100;

    public async Task SeedData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var defaultUser = await _userService.CreateDefaultUser();
        // Seed with service-created data
    }
}
```

## Troubleshooting

### Common Issues

1. **Assembly Not Found**: Verify assembly search patterns match your output files
2. **Connection Timeout**: Increase `OperationTimeoutSeconds` in options
3. **Priority Conflicts**: Use gaps between priority numbers (10, 20, 30)
4. **Duplicate Key Errors**: Implement proper existence checking in `HasData()`

### Debugging

Enable detailed logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "FluentCMS.DataSeeding.MongoDB": "Trace"
    }
  }
}
```

This will show:
- Assembly scanning results
- Component discovery and instantiation
- Priority validation
- Execution timing and results

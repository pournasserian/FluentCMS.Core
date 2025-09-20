# FluentCMS.DataSeeding - System Patterns & Architecture

## System Architecture

### High-Level Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                   ASP.NET Core Application                   │
├─────────────────────────────────────────────────────────────┤
│  Program.cs: services.AddSqliteDataSeeding(...)            │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│              FluentCMS.DataSeeding.Sqlite                   │
├─────────────────────────────────────────────────────────────┤
│  • ServiceCollectionExtensions.cs                          │
│  • DataSeedingHostedService.cs                             │
│  • SqliteDataSeedingEngine.cs                              │
│  • SqliteSeedingContext.cs                                 │
│  • SqliteDataSeedingOptions.cs                             │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                FluentCMS.DataSeeding                        │
├─────────────────────────────────────────────────────────────┤
│  Abstractions/                                              │
│  • ISchemaValidator.cs                                      │
│  • IDataSeeder.cs                                           │
│  • ICondition.cs                                            │
│                                                             │
│  Models/                                                    │
│  • SeedingContext.cs                                        │
│  • SeedingResult.cs                                         │
│                                                             │
│  Conditions/                                                │
│  • EnvironmentCondition.cs                                  │
│  • ConfigurationCondition.cs                                │
│  • DataStateCondition.cs                                    │
│  • CompositeCondition.cs                                    │
│                                                             │
│  Engine/                                                    │
│  • AssemblyScanner.cs                                       │
│  • DependencyResolver.cs                                    │
└─────────────────────────────────────────────────────────────┘
```

### Component Relationships

#### Core Abstractions (Public API)
- **ISchemaValidator**: Schema validation and creation interface
- **IDataSeeder**: Data seeding interface with existence checking
- **ICondition**: Conditional execution interface
- **SeedingContext**: Database connection and configuration context
- **SeedingResult**: Execution result tracking

#### Implementation Layer (Internal)
- **AssemblyScanner**: Discovers implementations using wildcard patterns
- **DependencyResolver**: Orders components by priority and resolves dependencies
- **DataSeedingHostedService**: ASP.NET Core background service orchestrator
- **SqliteDataSeedingEngine**: Core execution engine for SQLite
- **SqliteSeedingContext**: SQLite-specific context implementation

## Key Design Patterns

### 1. Strategy Pattern
**Used for**: Database-specific implementations

```csharp
// Abstract seeding context
public abstract class SeedingContext
{
    public abstract IDbConnection GetConnection();
    public abstract T GetService<T>();
}

// SQLite-specific implementation
public class SqliteSeedingContext : SeedingContext
{
    public override IDbConnection GetConnection() => new SqliteConnection(_connectionString);
    public override T GetService<T>() => _serviceProvider.GetRequiredService<T>();
}
```

### 2. Factory Pattern
**Used for**: Service registration and configuration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteDataSeeding(
        this IServiceCollection services,
        string connectionString,
        Action<SqliteDataSeedingOptions> configure = null)
    {
        // Factory method creates and configures all required services
        var options = new SqliteDataSeedingOptions { ConnectionString = connectionString };
        configure?.Invoke(options);
        
        // Register services based on configuration
        services.AddSingleton(options);
        services.AddHostedService<DataSeedingHostedService>();
        services.AddScoped<SqliteDataSeedingEngine>();
        // ... auto-discover and register seeders
        
        return services;
    }
}
```

### 3. Template Method Pattern
**Used for**: Seeding execution flow

```csharp
public abstract class DataSeedingEngine
{
    // Template method defining execution sequence
    public async Task ExecuteSeeding(CancellationToken cancellationToken)
    {
        if (!await ShouldExecute(cancellationToken)) return;
        
        await ValidateAndCreateSchemas(cancellationToken);
        await SeedData(cancellationToken);
    }
    
    // Abstract methods for database-specific implementation
    protected abstract Task<bool> ShouldExecute(CancellationToken cancellationToken);
    protected abstract Task ValidateAndCreateSchemas(CancellationToken cancellationToken);
    protected abstract Task SeedData(CancellationToken cancellationToken);
}
```

### 4. Dependency Injection Pattern
**Used throughout**: All components use constructor injection

```csharp
public class DataSeedingHostedService : BackgroundService
{
    private readonly SqliteDataSeedingEngine _engine;
    private readonly ILogger<DataSeedingHostedService> _logger;
    
    public DataSeedingHostedService(
        SqliteDataSeedingEngine engine,
        ILogger<DataSeedingHostedService> logger)
    {
        _engine = engine;
        _logger = logger;
    }
}
```

### 5. Composite Pattern
**Used for**: Complex conditions

```csharp
public class CompositeCondition : ICondition
{
    private readonly ICondition[] _conditions;
    private readonly LogicalOperator _operator;
    
    public async Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken)
    {
        return _operator switch
        {
            LogicalOperator.And => await EvaluateAll(_conditions, context, cancellationToken),
            LogicalOperator.Or => await EvaluateAny(_conditions, context, cancellationToken),
            _ => false
        };
    }
}
```

## Critical Implementation Patterns

### 1. Assembly Scanning Pattern
**Pattern**: Reflection-based discovery with wildcard matching

```csharp
public class AssemblyScanner
{
    public async Task<IEnumerable<Type>> ScanForTypes<T>(IEnumerable<string> patterns)
    {
        var assemblies = await DiscoverAssemblies(patterns);
        var types = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(T).IsAssignableFrom(t) && 
                       !t.IsInterface && 
                       !t.IsAbstract &&
                       HasPublicConstructor(t));
                       
        return types;
    }
    
    private async Task<IEnumerable<Assembly>> DiscoverAssemblies(IEnumerable<string> patterns)
    {
        var assemblies = new List<Assembly>();
        var appDomain = AppDomain.CurrentDomain;
        
        foreach (var pattern in patterns)
        {
            var files = Directory.GetFiles(appDomain.BaseDirectory, pattern, SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    assemblies.Add(assembly);
                }
                catch (Exception ex)
                {
                    // Log and continue - some files may not be .NET assemblies
                }
            }
        }
        
        return assemblies;
    }
}
```

### 2. Priority-Based Execution Pattern
**Pattern**: Simple integer ordering with dependency resolution

```csharp
public class DependencyResolver
{
    public IEnumerable<T> OrderByPriority<T>(IEnumerable<T> items) 
        where T : class, IPriorityItem
    {
        return items.OrderBy(item => item.Priority);
    }
    
    public async Task ExecuteInOrder<T>(
        IEnumerable<T> items, 
        Func<T, CancellationToken, Task> executor,
        CancellationToken cancellationToken)
    {
        var orderedItems = OrderByPriority(items);
        
        foreach (var item in orderedItems)
        {
            await executor(item, cancellationToken);
        }
    }
}

public interface IPriorityItem
{
    int Priority { get; }
}
```

### 3. Conditional Execution Pattern
**Pattern**: Configurable condition evaluation

```csharp
public class ConditionEvaluator
{
    public async Task<bool> EvaluateConditions(
        IEnumerable<ICondition> conditions,
        SeedingContext context,
        CancellationToken cancellationToken)
    {
        // All conditions must pass (AND logic)
        foreach (var condition in conditions)
        {
            if (!await condition.ShouldExecute(context, cancellationToken))
            {
                return false;
            }
        }
        
        return true;
    }
}
```

### 4. Error Handling Pattern
**Pattern**: Configurable fail-fast vs continue-on-error

```csharp
public class ExecutionEngine
{
    private readonly bool _ignoreExceptions;
    
    public async Task ExecuteWithErrorHandling<T>(
        IEnumerable<T> items,
        Func<T, CancellationToken, Task> executor,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            try
            {
                await executor(item, cancellationToken);
            }
            catch (Exception ex) when (_ignoreExceptions)
            {
                _logger.LogError(ex, "Error executing {Type}: {Message}", 
                    typeof(T).Name, ex.Message);
                // Continue with next item
            }
            // If _ignoreExceptions is false, exception bubbles up and stops execution
        }
    }
}
```

## Database Connection Management

### Pattern: Context-Based Connection Lifecycle
```csharp
public abstract class SeedingContext : IDisposable
{
    protected IDbConnection _connection;
    protected bool _disposed = false;
    
    public abstract IDbConnection GetConnection();
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _connection?.Dispose();
        }
        _disposed = true;
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

// Usage pattern in seeders
public async Task SeedData(SeedingContext context, CancellationToken cancellationToken)
{
    using var connection = context.GetConnection();
    await connection.OpenAsync(cancellationToken);
    
    using var command = connection.CreateCommand();
    command.CommandText = "INSERT INTO ...";
    await command.ExecuteNonQueryAsync(cancellationToken);
}
```

## Service Registration Patterns

### Auto-Discovery Registration
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteDataSeeding(
        this IServiceCollection services,
        string connectionString,
        Action<SqliteDataSeedingOptions> configure = null)
    {
        // Configure options
        var options = ConfigureOptions(connectionString, configure);
        services.AddSingleton(options);
        
        // Register core services
        RegisterCoreServices(services);
        
        // Auto-discover and register seeders/validators
        RegisterDiscoveredTypes(services, options);
        
        return services;
    }
    
    private static void RegisterDiscoveredTypes(IServiceCollection services, SqliteDataSeedingOptions options)
    {
        var scanner = new AssemblyScanner();
        
        // Register all schema validators
        var validators = scanner.ScanForTypes<ISchemaValidator>(options.AssemblySearchPatterns);
        foreach (var validatorType in validators)
        {
            services.AddScoped(typeof(ISchemaValidator), validatorType);
        }
        
        // Register all data seeders
        var seeders = scanner.ScanForTypes<IDataSeeder>(options.AssemblySearchPatterns);
        foreach (var seederType in seeders)
        {
            services.AddScoped(typeof(IDataSeeder), seederType);
        }
    }
}
```

## Extension Points

### 1. Custom Database Support
Implement database-specific classes:
- `{Database}SeedingContext : SeedingContext`
- `{Database}DataSeedingEngine : DataSeedingEngine`
- `{Database}DataSeedingOptions`
- `Add{Database}DataSeeding()` extension method

### 2. Custom Conditions
Implement `ICondition` interface:
```csharp
public class TimeBasedCondition : ICondition
{
    public Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken)
    {
        var hour = DateTime.Now.Hour;
        return Task.FromResult(hour >= 9 && hour <= 17); // Business hours only
    }
}
```

### 3. Custom Discovery Patterns
Override assembly scanning behavior by providing custom `AssemblyScanner` implementation.

## Performance Considerations

### 1. Lazy Loading Pattern
- Seeders instantiated only when needed
- Assembly scanning cached after first execution
- Database connections opened per operation, not per application

### 2. Memory Management
- Dispose pattern implemented throughout
- No static state retention
- Minimal object allocation during execution

### 3. Async/Await Pattern
- All database operations are asynchronous
- CancellationToken support throughout
- No blocking synchronous calls

This architecture provides a clean separation of concerns, extensible design patterns, and robust error handling while maintaining simplicity for common use cases.

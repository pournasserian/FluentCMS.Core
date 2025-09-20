# System Patterns - FluentCMS Core Provider System

## System Architecture

### Layered Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  Consumer Code using IEmailProvider, IStorageProvider, etc. │
├──────────────────────────────────────────────────────────────┤
│                   Provider Interfaces                       │
│  IEmailProvider  │ IStorageProvider │ ICacheProvider │ ...   │
├──────────────────────────────────────────────────────────────┤
│                 FluentCMS.Providers                          │
│  ┌─────────────────┐ ┌─────────────────┐ ┌──────────────────┐│
│  │ ProviderManager │ │ Provider        │ │ Provider         ││
│  │                 │ │ Discovery       │ │ Resolution       ││
│  └─────────────────┘ └─────────────────┘ └──────────────────┘│
├──────────────────────────────────────────────────────────────┤
│            FluentCMS.Providers.Abstractions                  │
│    IProvider │ IProviderModule │ ProviderModuleBase          │
├──────────────────────────────────────────────────────────────┤
│                 Repository Layer                             │
│  Configuration    │           Entity Framework               │
│  Repository       │           Repository                     │
└──────────────────────────────────────────────────────────────┘
```

## Core Design Patterns

### 1. Provider Pattern
**Purpose**: Abstraction for swappable service implementations

```csharp
// Base marker interface
public interface IProvider { }

// Specific provider interface
public interface IEmailProvider : IProvider
{
    Task SendEmailAsync(string to, string subject, string body);
}

// Implementation
public class SmtpProvider : IEmailProvider
{
    // Implementation details
}
```

### 2. Module Pattern
**Purpose**: Encapsulate provider registration and configuration

```csharp
public abstract class ProviderModuleBase<TProvider, TOptions> : ProviderModuleBase<TProvider>
{
    public abstract string Area { get; }
    public abstract string DisplayName { get; }
    public virtual void ConfigureServices(IServiceCollection services) { }
}
```

### 3. Repository Pattern
**Purpose**: Abstract storage mechanism for provider configuration

```csharp
public interface IProviderRepository
{
    Task<List<Provider>> GetAll(CancellationToken cancellationToken = default);
    Task<List<Provider>> GetByArea(string area, CancellationToken cancellationToken = default);
    // Other CRUD operations
}
```

### 4. Builder Pattern
**Purpose**: Fluent configuration of the provider system

```csharp
services.AddProviders(options =>
{
    options.AssemblyPrefixesToScan.Add("FluentCMS");
})
.UseConfiguration()  // or .UseEntityFramework()
```

### 5. Factory Pattern
**Purpose**: Dynamic provider instance creation

```csharp
private static object CreateProviderInstance(IServiceProvider serviceProvider, ProviderCatalog catalog)
{
    var constructors = catalog.Module.ProviderType.GetConstructors();
    // Constructor selection and instance creation logic
}
```

## Component Relationships

### Core Components Flow

```
ProviderDiscovery -> ProviderModuleCatalogCache -> ProviderManager -> ProviderCatalogCache -> Consumer
      ↑                       ↑                       ↑                      ↑
Assembly Scanning      Module Validation      Provider Resolution      Instance Caching
```

### Initialization Flow

1. **Assembly Scanning**: `ProviderDiscovery` scans assemblies for modules
2. **Module Registration**: `ProviderModuleCatalogCache` validates and stores modules
3. **Provider Loading**: `ProviderManager` loads provider configurations from repository
4. **Cache Population**: `ProviderCatalogCache` stores active providers for fast lookup
5. **DI Registration**: Active providers registered in service container

### Runtime Resolution Flow

1. **Service Resolution**: DI container requests provider interface
2. **Cache Lookup**: `ProviderCatalogCache.GetActiveCatalog(area)`
3. **Instance Creation**: Factory creates provider instance with options
4. **Service Return**: Provider instance returned to consumer

## Key Technical Decisions

### 1. Thread Safety Strategy

**Decision**: Use concurrent collections and immutable data structures
**Rationale**: High-performance concurrent access without locks

```csharp
internal sealed class ProviderCatalogCache
{
    private readonly ConcurrentDictionary<string, ImmutableList<ProviderCatalog>> _catalogs = new();
    private readonly ConcurrentDictionary<string, ProviderCatalog> _activeCatalogs = new();
}
```

### 2. Caching Strategy

**Decision**: Multi-level caching with invalidation
- Module-level caching (ProviderModuleCatalogCache)
- Provider catalog caching (ProviderCatalogCache)
- Assembly reflection caching

**Rationale**: Minimize expensive reflection and database operations

### 3. Configuration Flexibility

**Decision**: Support multiple storage backends
- Configuration files (appsettings.json)
- Database (Entity Framework)
- Custom repositories

**Rationale**: Different deployment scenarios have different needs

### 4. Provider Lifecycle Management

**Decision**: Singleton providers with dependency injection
**Rationale**: Providers should be stateless and reusable

### 5. Type Safety

**Decision**: Strongly-typed interfaces and options
**Rationale**: Compile-time safety and better developer experience

```csharp
public interface IProviderModule<TProvider, TOptions> : IProviderModule
{
    // Compile-time type safety
}
```

## Critical Implementation Paths

### 1. Provider Discovery Path

```
Assembly Loading → Module Detection → Interface Validation → Type Registration
```

**Critical Points**:
- Assembly load context management
- Exception handling for malformed assemblies
- Interface compliance validation

### 2. Provider Resolution Path

```
Area Lookup → Active Provider Check → Instance Creation → Dependency Injection
```

**Critical Points**:
- Thread-safe cache access
- Constructor parameter resolution
- Options deserialization

### 3. Configuration Change Path

```
Repository Update → Cache Invalidation → Provider Re-registration → DI Container Update
```

**Critical Points**:
- Atomic cache updates
- Service descriptor replacement
- Graceful provider switching

## Performance Optimizations

### 1. Assembly Scanning Optimization

```csharp
// Only scan assemblies with specific prefixes
options.AssemblyPrefixesToScan.Add("FluentCMS");

// Cache assembly reflection results
private static readonly ConcurrentDictionary<Assembly, List<Type>> _assemblyTypeCache = new();
```

### 2. Provider Resolution Optimization

```csharp
// O(1) lookup for active providers
public ProviderCatalog? GetActiveCatalog(string area)
{
    return _activeCatalogs.TryGetValue(area, out var catalog) ? catalog : null;
}
```

### 3. Memory Management

- Immutable collections prevent defensive copying
- Lazy loading of provider modules
- Proper disposal patterns for assembly loading

## Error Handling Patterns

### 1. Graceful Degradation

```csharp
// Continue loading other providers if one fails
public List<IProviderModule> GetProviderModules()
{
    // Exception handling per assembly/module
    // Log errors but continue processing
}
```

### 2. Validation Patterns

```csharp
private static void ValidateModule(IProviderModule module)
{
    if (string.IsNullOrEmpty(module.Area))
        throw new InvalidOperationException($"Provider module area cannot be null or empty.");
}
```

### 3. Consistent Error Responses

- InvalidOperationException for configuration errors
- ArgumentException for invalid parameters
- JsonException for malformed provider options

## Security Considerations

### 1. Assembly Loading Security

- Restrict assembly scanning to trusted prefixes
- Validate module interfaces before instantiation
- Sandboxed assembly loading contexts

### 2. Configuration Security

- Secure storage of sensitive provider options
- Validation of provider configurations
- Audit logging for provider changes

### 3. Runtime Security

- No reflection-based arbitrary code execution
- Validated constructor parameters
- Controlled service registration

## Extensibility Points

### 1. Custom Repositories

```csharp
public interface IProviderRepository
{
    // Implement for custom storage backends
}
```

### 2. Custom Discovery

```csharp
public class ProviderDiscoveryOptions
{
    public List<string> AssemblyPrefixesToScan { get; set; } = new();
    // Extensible discovery options
}
```

### 3. Custom Validation

```csharp
public abstract class ProviderModuleBase<TProvider, TOptions>
{
    public virtual void ConfigureServices(IServiceCollection services)
    {
        // Custom service registration
    }
}
```

## Testing Patterns

### 1. Mock Provider Pattern

```csharp
public class MockEmailProvider : IEmailProvider
{
    public Task SendEmailAsync(string to, string subject, string body)
    {
        // Mock implementation for testing
    }
}
```

### 2. Integration Testing

- In-memory provider repositories
- Test assembly scanning
- Configuration validation testing

### 3. Performance Testing

- Provider resolution benchmarks
- Concurrent access testing
- Memory usage validation

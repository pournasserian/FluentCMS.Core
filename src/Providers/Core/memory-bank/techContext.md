# Tech Context - FluentCMS Core Provider System

## Technology Stack

### Primary Technologies

#### .NET 9.0
- **Framework**: .NET 9.0 (latest LTS)
- **Language**: C# 12 with latest language features
- **Runtime**: Cross-platform (.NET Core runtime)

#### ASP.NET Core Integration
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Configuration**: Microsoft.Extensions.Configuration
- **Options Pattern**: Microsoft.Extensions.Options
- **Logging**: Microsoft.Extensions.Logging (integration ready)

#### Data Access
- **Entity Framework Core**: For database provider storage
- **System.Text.Json**: For configuration serialization/deserialization
- **Configuration Files**: appsettings.json support

#### Performance & Threading
- **System.Collections.Immutable**: Thread-safe collections
- **System.Collections.Concurrent**: High-performance concurrent access
- **System.Threading**: Cancellation token support

## Project Structure

### Solution Organization
```
Core.sln
├── FluentCMS.Providers.Abstractions/          # Core interfaces
├── FluentCMS.Providers/                        # Main implementation
└── FluentCMS.Providers.Repositories.EntityFramework/  # EF storage
```

### Key Dependencies

#### FluentCMS.Providers.Abstractions
```xml
<TargetFramework>net9.0</TargetFramework>
<Dependencies>
  - Microsoft.Extensions.DependencyInjection.Abstractions
</Dependencies>
```

#### FluentCMS.Providers
```xml
<TargetFramework>net9.0</TargetFramework>
<Dependencies>
  - FluentCMS.Providers.Abstractions
  - Microsoft.Extensions.DependencyInjection
  - Microsoft.Extensions.Configuration
  - Microsoft.Extensions.Options
  - System.Text.Json
  - System.Collections.Immutable
</Dependencies>
```

#### FluentCMS.Providers.Repositories.EntityFramework
```xml
<TargetFramework>net9.0</TargetFramework>
<Dependencies>
  - FluentCMS.Providers
  - FluentCMS.Repositories.EntityFramework
  - FluentCMS.DataSeeder.Abstractions
  - Microsoft.EntityFrameworkCore
</Dependencies>
```

## Development Environment

### Build System
- **SDK**: .NET 9.0 SDK
- **Build Target**: net9.0
- **IDE**: Visual Studio 2022 / VS Code / JetBrains Rider
- **Package Manager**: NuGet

### Source Control
- **Repository**: https://github.com/pournasserian/FluentCMS.Core.git
- **Branch Strategy**: Git Flow (main/develop/feature branches)
- **Latest Commit**: c08695f0e69bd63f3ac293fe2aaed194e08325c5

### Development Tools
- **Debugging**: Built-in .NET debugger support
- **Testing**: xUnit, NUnit, or MSTest compatible
- **Code Analysis**: Built-in Roslyn analyzers
- **Documentation**: XML comments + README files

## Technical Constraints

### Framework Constraints
- **Target Framework**: .NET 9.0 only (no .NET Framework support)
- **Language Version**: C# 12 features available
- **Assembly Loading**: Must work in various hosting environments
- **Threading**: Must be thread-safe for web applications

### Performance Constraints
- **Provider Resolution**: < 1ms for cached lookups
- **Memory Usage**: Minimal overhead for discovery and caching
- **Assembly Scanning**: One-time cost at startup
- **Reflection**: Cached to minimize performance impact

### Compatibility Constraints
- **ASP.NET Core**: Compatible with ASP.NET Core 9.0+
- **Dependency Injection**: Works with built-in DI container
- **Configuration**: Works with standard configuration providers
- **Database**: Multiple database providers through EF Core

## Architecture Patterns in Use

### Dependency Injection Patterns
```csharp
// Service registration patterns
services.AddProviders();  // Extension method pattern
services.AddTransient<IEmailProvider>();  // DI registration
services.Configure<ProviderOptions>();    // Options pattern
```

### Async/Await Patterns
```csharp
// All repository operations are async
Task<List<Provider>> GetAll(CancellationToken cancellationToken = default);
Task<Provider?> GetById(Guid id, CancellationToken cancellationToken = default);
```

### Generic Type Patterns
```csharp
// Strongly-typed provider modules
public abstract class ProviderModuleBase<TProvider, TOptions>
public interface IProviderModule<TProvider, TOptions>
```

### Factory Method Patterns
```csharp
// Dynamic provider instance creation
private static object CreateProviderInstance(IServiceProvider serviceProvider, ProviderCatalog catalog)
```

## Code Conventions

### Naming Conventions
- **Classes**: PascalCase (`ProviderManager`)
- **Interfaces**: IPascalCase (`IProviderRepository`) 
- **Methods**: PascalCase (`GetActiveByArea`)
- **Parameters**: camelCase (`cancellationToken`)
- **Private Fields**: _camelCase (`_catalogs`)

### Code Style
- **Accessibility**: Explicit accessibility modifiers
- **Async Methods**: No "Async" suffix (as per custom instructions)
- **Documentation**: Comments only (no XML documentation)
- **Nullable**: Nullable reference types enabled

### Error Handling
- **Exceptions**: Standard .NET exception types
- **Validation**: ArgumentException for parameter validation
- **Configuration**: InvalidOperationException for setup errors
- **Cancellation**: Proper CancellationToken support

## Performance Optimizations

### Caching Strategies
```csharp
// Multi-level caching
private readonly ConcurrentDictionary<string, ImmutableList<ProviderCatalog>> _catalogs;
private readonly ConcurrentDictionary<string, ProviderCatalog> _activeCatalogs;
private static readonly ConcurrentDictionary<Assembly, List<Type>> _assemblyTypeCache;
```

### Memory Management
- **Immutable Collections**: Prevent unnecessary copying
- **Lazy Loading**: Load providers only when needed
- **Assembly Loading**: Proper disposal patterns
- **Object Pooling**: Where applicable for high-frequency operations

### Thread Safety
- **Lock-Free**: Use ConcurrentDictionary instead of locks
- **Immutable Data**: Immutable collections for shared state
- **Atomic Operations**: Where thread safety is critical

## Integration Points

### ASP.NET Core Integration
```csharp
// Startup/Program.cs integration
builder.Services.AddProviders(options =>
{
    options.AssemblyPrefixesToScan.Add("FluentCMS");
});
```

### Entity Framework Integration
```csharp
// DbContext integration
services.AddDbContext<ProviderDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### Configuration System Integration
```csharp
// appsettings.json integration
{
  "Providers": {
    "Email": {
      "SmtpProvider": {
        "Active": true,
        "Options": { /* ... */ }
      }
    }
  }
}
```

## Testing Strategy

### Unit Testing
- **Framework**: xUnit recommended (or MSTest/NUnit)
- **Mocking**: MOQ or NSubstitute for interface mocking
- **Isolation**: Test individual components in isolation
- **Coverage**: Aim for 90%+ code coverage

### Integration Testing
- **In-Memory Database**: For repository testing
- **Test Containers**: For full database integration tests
- **Assembly Loading**: Test provider discovery mechanisms
- **Configuration**: Test various configuration scenarios

### Performance Testing
- **Benchmarking**: BenchmarkDotNet for performance testing
- **Load Testing**: Concurrent provider resolution
- **Memory Profiling**: Memory usage validation
- **Stress Testing**: High-load scenarios

## Deployment Considerations

### Package Distribution
- **NuGet Packages**: Individual packages for each component
- **Versioning**: Semantic versioning (SemVer)
- **Dependencies**: Minimal dependency footprint
- **Compatibility**: Version compatibility matrix

### Runtime Requirements
- **.NET Runtime**: .NET 9.0 runtime required
- **Database**: Optional (for Entity Framework storage)
- **File System**: Read access for assembly scanning
- **Memory**: Minimal additional memory overhead

### Configuration Management
- **Environment Variables**: Support for environment-specific config
- **Configuration Files**: Multiple file format support
- **Database Configuration**: For dynamic provider management
- **Secrets Management**: Integration with ASP.NET Core secrets

## Security Considerations

### Assembly Loading Security
- **Trusted Assemblies**: Restrict scanning to known prefixes
- **Sandboxing**: AssemblyLoadContext for isolation
- **Validation**: Interface compliance before instantiation

### Configuration Security
- **Sensitive Data**: Secure storage of provider credentials
- **Validation**: Input validation for provider options
- **Audit Trail**: Logging of provider configuration changes

### Runtime Security
- **Reflection Limits**: Controlled reflection usage
- **Parameter Validation**: Constructor parameter validation
- **Service Registration**: Controlled DI registration

## Monitoring and Diagnostics

### Logging Integration
```csharp
// Built-in logging support
private readonly ILogger<ProviderManager> _logger;
```

### Performance Metrics
- **Provider Resolution Time**: Track lookup performance
- **Memory Usage**: Monitor cache memory consumption
- **Error Rates**: Track provider failures and exceptions

### Health Checks
- **Provider Availability**: Health check integration
- **Configuration Validation**: Startup configuration checks
- **Database Connectivity**: For EF-based storage

## Future Technology Considerations

### Planned Upgrades
- **.NET Versions**: Support for future .NET versions
- **Performance**: Source generators for compile-time optimization
- **Cloud Integration**: Azure/AWS specific optimizations

### Extensibility
- **Plugin Architecture**: Support for external provider packages
- **Configuration Providers**: Custom configuration sources
- **Monitoring Integration**: APM tool integration
- **Container Support**: Docker/Kubernetes optimization

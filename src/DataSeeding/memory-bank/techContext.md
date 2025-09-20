# FluentCMS.DataSeeding - Technical Context

## Target Framework & Dependencies

### Framework Requirements
- **.NET 9+**: Latest LTS framework with performance improvements
- **ASP.NET Core 9+**: For hosted service integration and dependency injection
- **C# 12+**: Modern language features and nullable reference types

### Core Dependencies

#### Primary Dependencies
```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
```

#### Development Dependencies
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="Moq" Version="4.20.69" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

## Project Structure & Organization

### Solution Structure
```
FluentCMS.DataSeeding.sln
├── src/
│   ├── FluentCMS.DataSeeding/                 # Core abstractions library
│   └── FluentCMS.DataSeeding.Sqlite/          # SQLite implementation
├── samples/
│   ├── BasicUsage/                            # Simple example
│   ├── AdvancedConfiguration/                 # Complex scenarios
│   └── ModularApplication/                    # Multi-assembly example
├── tests/
│   ├── FluentCMS.DataSeeding.Tests/           # Core library tests
│   └── FluentCMS.DataSeeding.Sqlite.Tests/    # SQLite implementation tests
├── docs/
│   ├── README.md
│   ├── CHANGELOG.md
│   └── api/                                   # API documentation
└── .github/
    └── workflows/                             # CI/CD pipelines
```

### Assembly Design

#### FluentCMS.DataSeeding (Core Library)
- **Target**: net9.0 for broader compatibility
- **Purpose**: Abstract interfaces and base implementations
- **Dependencies**: Minimal - only essential .NET abstractions
- **Public API Surface**: All interfaces, models, and built-in conditions

#### FluentCMS.DataSeeding.Sqlite (Implementation)
- **Target**: net9.0 (requires ASP.NET Core features)
- **Purpose**: SQLite-specific implementation and DI integration
- **Dependencies**: Core library + SQLite + ASP.NET Core
- **Public API Surface**: Extension methods and configuration classes only

## Development Environment

### IDE & Tools
- **Primary IDE**: Visual Studio 2022 or Visual Studio Code
- **SDK**: .NET 9 SDK
- **Database Tools**: SQLite Browser, DB Browser for SQLite
- **Version Control**: Git with GitHub integration

### Development Workflow
1. **Feature Development**: Feature branches from main
2. **Testing**: Unit tests for all public APIs, integration tests for database operations
3. **Code Review**: All changes require PR review
4. **CI/CD**: Automated builds, tests, and package generation
5. **Documentation**: Inline comments and README updates

### Local Development Setup
```bash
# Clone repository
git clone https://github.com/fluentcms/FluentCMS.DataSeeding.git
cd FluentCMS.DataSeeding

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run sample application
cd samples/BasicUsage
dotnet run
```

## Technical Constraints & Design Decisions

### Database Support Strategy
- **Primary Target**: SQLite for simplicity and no infrastructure requirements
- **Future Targets**: SQL Server, PostgreSQL, MySQL via separate packages
- **Architecture**: Database-agnostic core with database-specific implementations
- **Connection Management**: No connection pooling - leverage existing application infrastructure

### Assembly Loading & Discovery
- **Pattern Matching**: Simple wildcard patterns using `Directory.GetFiles()`
- **Security**: No dynamic assembly compilation - only load existing assemblies
- **Performance**: Lazy loading with caching after first discovery
- **Error Handling**: Graceful failure for assemblies that can't be loaded

### Threading & Concurrency
- **Hosted Service**: Single background thread execution during startup
- **Async Pattern**: All operations async with CancellationToken support
- **No Parallelism**: Sequential execution to maintain dependency order
- **Thread Safety**: No shared mutable state between seeding operations

### Memory Management
- **Disposal Pattern**: Proper IDisposable implementation throughout
- **Object Lifetime**: 
  - Transient: Database connections (per operation)
  - Scoped: Seeders and validators (per execution cycle)
  - Singleton: Configuration and options
- **No Caching**: Minimal memory footprint, no long-lived caches

## Configuration Architecture

### Options Pattern
```csharp
public class SqliteDataSeedingOptions
{
    // Connection configuration
    public string ConnectionString { get; set; }
    
    // Discovery configuration
    public List<string> AssemblySearchPatterns { get; } = new();
    
    // Execution configuration
    public List<ICondition> Conditions { get; } = new();
    public bool IgnoreExceptions { get; set; } = false;
    
    // Logging configuration
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
```

### Environment-Specific Configuration
```json
// appsettings.Development.json
{
  "DataSeeding": {
    "ConnectionString": "Data Source=development.db",
    "EnableSeeding": true,
    "AssemblyPatterns": ["MyApp.*.dll", "MyApp.Modules.*.dll"]
  }
}

// appsettings.Production.json
{
  "DataSeeding": {
    "EnableSeeding": false
  }
}
```

## Error Handling Strategy

### Exception Hierarchy
```csharp
public class DataSeedingException : Exception
{
    public string SeederType { get; }
    public int Priority { get; }
}

public class SchemaValidationException : DataSeedingException { }
public class AssemblyDiscoveryException : DataSeedingException { }
public class ConditionEvaluationException : DataSeedingException { }
```

### Logging Strategy
- **Structured Logging**: Use Microsoft.Extensions.Logging with structured data
- **Log Levels**:
  - Trace: Detailed execution flow
  - Debug: Assembly discovery and type registration
  - Information: Seeding start/completion, condition evaluation
  - Warning: Recoverable errors, skipped operations
  - Error: Seeding failures, unhandled exceptions
  - Critical: System-level failures

### Error Recovery
- **Fail-Fast (Default)**: Stop on first error for predictable behavior
- **Continue-On-Error**: Log and continue for resilient execution
- **Idempotent Design**: Safe to retry operations without side effects

## Performance Characteristics

### Startup Performance
- **Assembly Scanning**: O(n) where n = number of assemblies matching patterns
- **Type Discovery**: O(m) where m = number of types in discovered assemblies
- **Registration**: O(k) where k = number of discovered seeders/validators
- **Expected Impact**: <100ms for typical applications with <10 assemblies

### Runtime Performance
- **Execution Order**: O(s log s) where s = number of seeders (due to sorting)
- **Database Operations**: Dependent on database and data volume
- **Memory Usage**: Minimal - no large object retention
- **Expected Impact**: <5s for typical development database seeding

### Scalability Considerations
- **Assembly Patterns**: Avoid overly broad patterns that scan unnecessary assemblies
- **Priority Gaps**: Use priority gaps (10, 20, 30) to allow insertion without reordering
- **Database Size**: Consider data volume for production-like seeding scenarios

## Security Considerations

### Assembly Loading Security
- **No Dynamic Compilation**: Only load pre-compiled assemblies
- **Pattern Validation**: Validate assembly search patterns for path traversal
- **Exception Handling**: Don't expose internal paths in error messages

### Database Security
- **Connection Strings**: Support for environment variables and configuration providers
- **SQL Injection**: Use parameterized queries in all seeder examples
- **Permissions**: Document minimum required database permissions

### Environment Protection
- **Condition Guards**: Multiple layers of condition checking
- **Configuration Validation**: Validate environment-specific settings
- **Audit Logging**: Log all seeding operations for audit trails

## Testing Strategy

### Unit Testing
- **Core Interfaces**: Mock implementations for all abstractions
- **Condition Logic**: Test all condition implementations
- **Assembly Scanning**: Test pattern matching with known assemblies
- **Priority Ordering**: Test execution order with various priority configurations

### Integration Testing
- **Database Operations**: Real SQLite database with temporary files
- **End-to-End**: Full seeding workflow with sample seeders
- **Error Scenarios**: Test exception handling and recovery

### Testing Infrastructure
```csharp
// Base test class for database tests
public abstract class DatabaseTestBase : IDisposable
{
    protected string ConnectionString { get; }
    protected SqliteSeedingContext Context { get; }
    
    protected DatabaseTestBase()
    {
        ConnectionString = $"Data Source={Path.GetTempFileName()}";
        Context = new SqliteSeedingContext(ConnectionString, serviceProvider);
    }
    
    public virtual void Dispose()
    {
        Context?.Dispose();
        if (File.Exists(ConnectionString))
            File.Delete(ConnectionString);
    }
}
```

## Build & Deployment

### Build Configuration
```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
  <LangVersion>12.0</LangVersion>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

### Package Configuration
```xml
<PropertyGroup>
  <PackageId>FluentCMS.DataSeeding.Sqlite</PackageId>
  <Version>1.0.0</Version>
  <Authors>FluentCMS Team</Authors>
  <Description>SQLite implementation of FluentCMS.DataSeeding library</Description>
  <PackageTags>aspnetcore;sqlite;seeding;database</PackageTags>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
</PropertyGroup>
```

### CI/CD Pipeline
1. **Build**: Multi-target build for all supported frameworks
2. **Test**: Unit and integration tests with code coverage
3. **Pack**: NuGet package generation with semantic versioning
4. **Deploy**: Automated deployment to NuGet.org on release tags

This technical foundation provides a robust, maintainable, and extensible platform for database seeding while maintaining compatibility with modern .NET development practices.

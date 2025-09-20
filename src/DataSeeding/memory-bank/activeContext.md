# FluentCMS.DataSeeding - Active Context

## Current Work Focus

### Primary Objective
**Initial Implementation Phase**: Building the core abstractions and SQLite implementation of the FluentCMS.DataSeeding library from the ground up.

### Immediate Priorities
1. **Core Abstractions**: Implement the fundamental interfaces (`ISchemaValidator`, `IDataSeeder`, `ICondition`)
2. **SQLite Implementation**: Create database-specific classes for SQLite integration
3. **Assembly Discovery**: Build the auto-discovery mechanism with wildcard pattern support
4. **ASP.NET Core Integration**: Implement hosted service and dependency injection setup

### Current Development Phase
- **Phase**: Foundation Building
- **Status**: Starting implementation from detailed specifications
- **Focus Area**: Core library structure and basic SQLite implementation
- **Next Milestone**: Working basic seeding example with auto-discovery

## Recent Changes & Decisions

### Architecture Decisions Made
1. **Two-Library Approach**: 
   - `FluentCMS.DataSeeding` (net9.0) for abstractions
   - `FluentCMS.DataSeeding.Sqlite` (net9.0) for implementation
   - **Rationale**: Broader compatibility for abstractions, modern features for implementation

2. **Priority-Based Execution**:
   - Simple integer-based ordering (lower numbers execute first)
   - **Rationale**: Easy to understand and implement, allows for dependency management

3. **Auto-Discovery via Assembly Scanning**:
   - Wildcard pattern matching using `Directory.GetFiles()`
   - **Rationale**: Convention over configuration, reduces manual registration

4. **Conditional Execution Framework**:
   - Multiple conditions with AND logic by default
   - **Rationale**: Safety-first approach, prevents accidental production seeding

### Key Implementation Patterns Established
- **Async/Await Throughout**: All database operations async with CancellationToken
- **Disposal Pattern**: Proper resource management for database connections
- **Strategy Pattern**: Database-specific implementations behind common abstractions
- **Template Method**: Consistent execution flow across database engines

## Active Implementation Strategy

### Development Approach
1. **Interface-First Design**: Define all abstractions before implementations
2. **SQLite-First Implementation**: Use SQLite as reference implementation
3. **Test-Driven Approach**: Unit tests for abstractions, integration tests for database operations
4. **Sample-Driven Validation**: Create working examples to validate design decisions

### Code Organization Philosophy
- **Clean Separation**: Public API surface vs internal implementation
- **Convention Over Configuration**: Sensible defaults with minimal required setup
- **Extensibility Points**: Clear extension patterns for custom scenarios
- **Developer Experience**: Simple interfaces, auto-discovery, clear error messages

## Current Technical Decisions

### Assembly Discovery Implementation
```csharp
// Chosen approach: Simple wildcard matching
var files = Directory.GetFiles(appDomain.BaseDirectory, pattern, SearchOption.TopDirectoryOnly);
```
- **Alternative Considered**: AppDomain.CurrentDomain.GetAssemblies() (rejected - doesn't find unloaded assemblies)
- **Rationale**: Explicit file-based discovery gives more control and matches user expectations

### Error Handling Strategy
```csharp
// Configurable fail-fast vs continue-on-error
public bool IgnoreExceptions { get; set; } = false; // Default: fail-fast
```
- **Alternative Considered**: Always continue on error (rejected - unpredictable behavior)
- **Rationale**: Fail-fast by default ensures predictable behavior, but allow resilient mode when needed

### Connection Management Pattern
```csharp
// Per-operation connection creation
using var connection = context.GetConnection();
```
- **Alternative Considered**: Long-lived connections (rejected - resource management complexity)
- **Rationale**: Simple, predictable, leverages existing application connection infrastructure

## Important Patterns & Preferences

### Code Style Preferences
- **No "Async" Suffix**: `SeedData()` not `SeedDataAsync()` - follows Microsoft guidelines
- **CancellationToken Default**: All async methods have `CancellationToken cancellationToken = default`
- **Inline Comments**: Prefer inline comments over XML documentation
- **Explicit Interface Implementation**: Clear separation between public and internal APIs

### Design Patterns in Use
1. **Factory Pattern**: Service registration and configuration
2. **Strategy Pattern**: Database-specific implementations
3. **Template Method**: Consistent execution flow
4. **Composite Pattern**: Complex conditional logic
5. **Repository Pattern**: (Implicit) Schema validators manage schema concerns

### Naming Conventions
- **Interfaces**: `I{Purpose}` (e.g., `IDataSeeder`, `ISchemaValidator`)
- **Implementations**: `{Database}{Purpose}` (e.g., `SqliteDataSeedingEngine`)
- **Options**: `{Database}DataSeedingOptions`
- **Extensions**: `Add{Database}DataSeeding()`

## Current Learning & Insights

### Discovery Process Insights
- **Wildcard Patterns**: Users think in terms of assembly patterns, not reflection APIs
- **Priority Gaps**: Recommend 10-point gaps (10, 20, 30) to allow future insertion
- **Error Recovery**: Most users prefer fail-fast for development, continue-on-error for staging

### Integration Insights
- **Hosted Service Timing**: Execute during startup but after DI container is fully configured
- **Condition Evaluation**: Environment and configuration checks are most common
- **Schema vs Data Separation**: Clear separation helps with dependency ordering

### Developer Experience Insights
- **Single Registration**: `AddSqliteDataSeeding()` should be the only required call
- **Auto-Discovery**: Zero manual registration is a key differentiator
- **Predictable Execution**: Developers need to understand exactly what runs and when

## Next Steps & Immediate Actions

### Implementation Roadmap
1. **Core Interfaces** (Next):
   - Define `ISchemaValidator`, `IDataSeeder`, `ICondition`
   - Create `SeedingContext` and `SeedingResult` models
   - Implement built-in conditions

2. **SQLite Implementation**:
   - `SqliteSeedingContext` with connection management
   - `SqliteDataSeedingEngine` with execution flow
   - `SqliteDataSeedingOptions` configuration

3. **Discovery Engine**:
   - `AssemblyScanner` with wildcard pattern support
   - `DependencyResolver` for priority-based ordering
   - Integration with DI container

4. **ASP.NET Core Integration**:
   - `DataSeedingHostedService` background service
   - `ServiceCollectionExtensions` with auto-registration
   - Configuration binding and validation

5. **Sample Applications**:
   - Basic usage example
   - Advanced configuration scenario
   - Multi-assembly modular example

### Key Questions to Validate
- Does auto-discovery work with typical project structures?
- Is priority-based ordering intuitive for developers?
- Are conditional patterns flexible enough for real scenarios?
- Is the separation between core and implementation clean?

This active context drives the immediate implementation work and ensures decisions align with the overall project vision and user experience goals.

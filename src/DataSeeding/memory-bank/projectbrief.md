# FluentCMS.DataSeeding - Project Brief

## Project Identity
**FluentCMS.DataSeeding** is a flexible and extensible database seeding library for ASP.NET Core applications, providing database-agnostic seeding capabilities with auto-discovery, priority-based execution, and conditional seeding.

## Core Requirements

### Functional Requirements
- **Database Agnostic Architecture**: Support multiple database engines through extensible design
- **Auto-Discovery System**: Automatically find and register seeders and validators from assemblies
- **Priority-Based Execution**: Simple integer-based ordering for controlled execution sequence
- **Conditional Seeding**: Environment and configuration-driven execution control
- **Schema Management**: Validate and create database schemas as needed
- **Data Seeding**: Insert initial/test data with existence checking

### Technical Requirements
- **.NET 9+** and **ASP.NET Core 9+** support
- **Hosted Service Integration**: Automatic execution at application startup
- **Asynchronous Operations**: All database operations must be async with CancellationToken support
- **Dependency Injection**: Full DI container integration
- **Configurable Error Handling**: Choice between fail-fast or continue-on-error strategies

### Key Design Principles
1. **Convention Over Configuration**: Minimal setup with sensible defaults
2. **Clean Public API**: Hide implementation complexity behind simple interfaces
3. **Extensibility**: Support for custom conditions, validators, and seeders
4. **Developer Experience**: Easy to understand, implement, and debug

## Success Criteria
- Library can be integrated with a single `AddSqliteDataSeeding()` call
- Developers can create seeders by implementing simple interfaces
- Assembly scanning works with flexible wildcard patterns
- Priority-based execution ensures correct dependency order
- Conditional execution prevents unwanted seeding in production
- SQLite implementation serves as reference for other database engines

## Project Scope

### In Scope
- Core abstraction layer (`ISchemaValidator`, `IDataSeeder`, `ICondition`)
- SQLite implementation as primary target
- Assembly scanning with wildcard pattern support
- Built-in conditions (Environment, Configuration, Data State, Composite)
- Hosted service for automatic startup execution
- Comprehensive configuration options

### Future Scope
- Additional database engines (SQL Server, MySQL, PostgreSQL, MongoDB)
- Data import from external sources (CSV, JSON, XML)
- Performance metrics and reporting
- Advanced schema migration capabilities

### Out of Scope
- Full migration framework (use EF Core migrations for that)
- Data transformation or ETL capabilities
- Real-time data synchronization
- Multi-tenant seeding strategies

## Key Interfaces

```csharp
// Core seeding interface
public interface IDataSeeder
{
    int Priority { get; }
    Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default);
    Task SeedData(SeedingContext context, CancellationToken cancellationToken = default);
}

// Schema management interface  
public interface ISchemaValidator
{
    int Priority { get; }
    Task<bool> ValidateSchema(SeedingContext context, CancellationToken cancellationToken = default);
    Task CreateSchema(SeedingContext context, CancellationToken cancellationToken = default);
}

// Conditional execution interface
public interface ICondition
{
    Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default);
}
```

## Implementation Strategy
1. Build core abstractions first
2. Implement SQLite-specific components
3. Create assembly scanning and discovery engine
4. Add built-in condition implementations
5. Integrate with ASP.NET Core hosting
6. Test with real-world scenarios
7. Document usage patterns and examples

This project enables consistent, reliable database seeding across development, testing, and staging environments while maintaining clean separation between schema management and data seeding concerns.

# FluentCMS.DataSeeding - Product Context

## Why This Project Exists

### The Problem
Database seeding in ASP.NET Core applications is currently fragmented and inconsistent:

1. **Manual Setup Complexity**: Developers write custom seeding code in `Program.cs` or startup classes, leading to cluttered initialization logic
2. **Database-Specific Solutions**: Most seeding approaches are tightly coupled to specific ORMs (Entity Framework) or databases
3. **No Standardization**: Each project implements seeding differently, making knowledge transfer and best practices difficult
4. **Environment Management**: Determining when and how to seed data across development/staging/production environments is error-prone
5. **Dependency Ordering**: Ensuring tables are created and seeded in correct dependency order requires manual coordination
6. **Discovery Problems**: As applications grow, finding and managing all seeding logic becomes challenging

### Market Gap
Existing solutions have limitations:
- **Entity Framework Migrations**: Great for schema but limited for flexible data seeding patterns
- **Custom Solutions**: Every project reinvents the wheel with inconsistent approaches
- **Database-Specific Tools**: Lock applications into specific database technologies

## The Solution

### Core Value Proposition
FluentCMS.DataSeeding provides a **standardized, discoverable, and extensible** approach to database seeding that works across database technologies while integrating seamlessly with ASP.NET Core applications.

### Key Benefits

#### For Individual Developers
- **Simple Interface**: Implement `IDataSeeder` or `ISchemaValidator` - that's it
- **Auto-Discovery**: No manual registration - just implement interfaces and they're found automatically
- **Clear Separation**: Schema validation/creation separate from data seeding
- **Priority Control**: Simple integer-based ordering for dependency management
- **Environment Safety**: Built-in conditions prevent accidental production seeding

#### For Development Teams
- **Consistent Patterns**: Same approach across all projects and team members
- **Modular Organization**: Seeders can be organized by domain/feature in separate assemblies
- **Version Control Friendly**: Each seeder is a separate class, reducing merge conflicts
- **Testing Support**: Easy to test individual seeders in isolation

#### For Application Architecture
- **Database Agnostic**: Switch between SQLite, SQL Server, etc. without changing seeding logic
- **Microservice Ready**: Each service can have its own seeding assemblies
- **Plugin Architecture**: Third-party packages can include their own seeders
- **Startup Integration**: Automatic execution during application startup with hosted services

## How It Should Work

### Developer Experience Journey

#### 1. Initial Setup (1 minute)
```csharp
// In Program.cs - single line integration
services.AddSqliteDataSeeding(connectionString, options =>
{
    options.AssemblySearchPatterns.Add("MyApp.*.dll");
    options.Conditions.Add(new EnvironmentCondition(env, e => e.IsDevelopment()));
});
```

#### 2. Creating a Schema Validator (5 minutes)
```csharp
public class UserSchemaValidator : ISchemaValidator
{
    public int Priority => 10;
    
    public async Task<bool> ValidateSchema(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Simple table existence check
    }
    
    public async Task CreateSchema(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Create tables if needed
    }
}
```

#### 3. Creating a Data Seeder (5 minutes)
```csharp
public class AdminUserSeeder : IDataSeeder
{
    public int Priority => 20; // After schema creation
    
    public async Task<bool> HasData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Check if admin user exists
    }
    
    public async Task SeedData(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Create admin user
    }
}
```

#### 4. Automatic Execution
- Application starts
- Conditions evaluated (environment, configuration, etc.)
- If conditions pass, seeding executes automatically
- Schema validators run first (by priority)
- Data seeders run second (by priority)
- Developers see clear logs of what happened

### User Experience Goals

#### Discoverability
- **Zero Registration**: Implementing interface is sufficient
- **Assembly Scanning**: Flexible wildcard patterns find seeders across multiple assemblies
- **Clear Logging**: Developers know exactly what ran and in what order

#### Predictability  
- **Priority-Based**: Lower numbers run first, providing deterministic execution order
- **Conditional Logic**: Clear rules about when seeding runs
- **Idempotent Operations**: Safe to run multiple times

#### Flexibility
- **Custom Conditions**: Developers can create complex execution rules
- **Database Agnostic**: Same code works across database technologies
- **Modular Design**: Seeders can be packaged in separate libraries

#### Safety
- **Environment Guards**: Built-in protection against accidental production seeding
- **Exception Handling**: Configurable fail-fast or continue-on-error behavior
- **Existence Checking**: Always check before seeding to prevent duplicates

## Success Scenarios

### Scenario 1: New Project Setup
Developer creates new ASP.NET Core project, adds FluentCMS.DataSeeding, implements a few seeders, and has a fully seeded development database ready for team members.

### Scenario 2: Modular Application
Large application with multiple feature modules, each containing their own seeders. Auto-discovery finds all seeders across assemblies, executes them in correct order.

### Scenario 3: Environment Management
Same codebase deploys to dev/staging/production with different seeding behavior based on environment conditions, without code changes.

### Scenario 4: Database Migration
Team switches from SQLite to SQL Server - only the configuration line changes, all seeding logic remains the same.

### Scenario 5: Third-Party Integration
Installing a NuGet package that includes its own seeders - they automatically integrate without additional configuration.

## User Personas

### Primary: Application Developer
- Needs simple, reliable way to seed development/test data
- Wants consistent patterns across projects
- Values auto-discovery and minimal configuration
- Prioritizes safety and predictability

### Secondary: DevOps Engineer
- Needs control over when seeding runs in different environments
- Wants clear logging and error handling
- Values configuration-driven behavior
- Prioritizes deployment reliability

### Tertiary: Library Author
- Wants to package seeders with their libraries
- Needs extensible architecture for custom scenarios
- Values clean abstraction layers
- Prioritizes integration simplicity

## Competitive Advantages

1. **Database Agnostic**: Unlike EF Core-specific solutions
2. **Auto-Discovery**: Unlike manual registration approaches
3. **ASP.NET Core Native**: Built for modern .NET applications
4. **Extensible Architecture**: Unlike rigid, one-size-fits-all tools
5. **Production Safety**: Unlike general-purpose seeding tools
6. **Modular Design**: Unlike monolithic seeding solutions

This product transforms database seeding from a custom, error-prone task into a standardized, reliable, and discoverable capability that scales with application complexity.

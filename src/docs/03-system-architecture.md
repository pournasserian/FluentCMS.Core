# System Architecture

## Component Diagram

```
+---------------------+      +--------------------+      +--------------------+
| Provider Controller |----->| Provider Manager   |----->| Provider Repository|
+---------------------+      +--------------------+      +--------------------+
                              |            |               |
                              |            |               v
                              v            v        +---------------+
                     +----------------+  +-----------------+  |     Database   |
                     | Assembly Loader|  | Dynamic Service |  +---------------+
                     +----------------+  |    Provider     |
                              |          +-----------------+
                              v                   |
                     +----------------+           |
                     | Provider       |<----------+
                     | Assemblies     |
                     +----------------+
```

## Data Model

```
+-----------------+      +------------------------+      +------------------------+
| ProviderTypes   |----->| ProviderImplementations|----->| ProviderConfigurations |
+-----------------+      +------------------------+      +------------------------+
| Id              |      | Id                     |      | Id                     |
| Name            |      | ProviderTypeId         |      | ImplementationId       |
| FullTypeName    |      | Name                   |      | ConfigurationJson      |
| AssemblyName    |      | Description            |      | UpdatedAt              |
| CreatedAt       |      | Version                |      +------------------------+
| UpdatedAt       |      | FullTypeName           |
+-----------------+      | AssemblyPath           |
                         | IsInstalled            |
                         | IsActive               |
                         | HealthStatus           |
                         | HealthMessage          |
                         | LastHealthCheckAt      |
                         | InstalledAt            |
                         | ActivatedAt            |
                         | UpdatedAt              |
                         +------------------------+
```

## Core Components

### Provider Abstractions

These are the core interfaces that define the provider system:

- **IProvider**: Flag interface for all providers
- **IProviderLifecycle**: Interface for provider lifecycle hooks
- **IProviderHealth**: Interface for health checking and monitoring
- **IProviderWithOptions<T>**: Interface for providers with configuration

### Provider Manager

The Provider Manager is the central component that handles:

- Provider discovery
- Provider activation/deactivation
- Provider installation/uninstallation
- Configuration management
- Health status tracking

### Assembly Loading

The Assembly Loading component handles:

- Loading provider assemblies in isolated contexts
- Resolving provider dependencies
- Safe unloading of provider assemblies

### Dynamic Service Provider

The Dynamic Service Provider handles:

- Runtime modifications to the DI container
- Registering/unregistering providers
- Managing provider instance lifecycles

### Provider Repository

The Provider Repository handles:

- Persistence of provider metadata
- Storage and retrieval of provider configurations
- Tracking of provider health status

### API Controllers

The API Controllers provide endpoints for:

- Listing available providers
- Activating/deactivating providers
- Configuring providers
- Installing/uninstalling providers
- Checking provider health

## Technical Stack

- **Language**: C# 10+
- **Framework**: ASP.NET Core 6.0+
- **Database**: SQLite with Entity Framework Core
- **Assembly Loading**: AssemblyLoadContext
- **Dependency Injection**: ASP.NET Core DI with custom extensions
- **API**: ASP.NET Core Web API

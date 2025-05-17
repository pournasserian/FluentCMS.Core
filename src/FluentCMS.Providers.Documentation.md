# FluentCMS Provider System Architecture

## Overview

The FluentCMS Provider System is a flexible, modular system for developing, loading, and managing pluggable components (providers) at runtime. The system allows developers to create multiple implementations of various service types, with the ability to switch between implementations dynamically without restarting the application.

## Key Components

### Core Architecture

1. **Abstractions Layer** (`FluentCMS.Providers.Abstractions`)
   - `IProvider`: Base marker interface for all providers
   - `IProviderLifecycle`: Interface for managing provider lifecycle (initialize, activate, deactivate, uninstall)
   - `IProviderHealth`: Interface for health monitoring
   - `IProviderWithOptions<T>`: Interface for providers with configuration options
   - `ProviderBase`: Base class for provider implementations

2. **Data Layer** (`FluentCMS.Providers.Data`)
   - `ProviderDbContext`: EF Core DbContext for provider data
   - Data models for provider metadata, implementation details, and configurations
   - `IProviderRepository`: Repository interface for data access
   - `ProviderRepository`: Implementation of the repository interface

3. **Core Provider System** (`FluentCMS.Providers`)
   - `ProviderAssemblyLoadContext`: Isolated assembly loading context
   - `ProviderAssemblyManager`: Management of provider assemblies
   - `DynamicServiceProvider`: Dynamic service registration
   - `ProviderManager`: Central manager implementing `IProviderManager`
   - `ServiceCollectionExtensions`: DI registration extensions

4. **API Layer** (`FluentCMS.Api.Controllers`)
   - `ProvidersController`: RESTful API endpoints for provider management

### Sample Providers

1. **Email Provider** (`FluentCMS.Providers.Email`)
   - `IEmailProvider`: Interface for email providers
   - `SmtpEmailProvider`: SMTP implementation of email provider
   - `SmtpEmailOptions`: Configuration options for SMTP provider

## Provider Lifecycle

Providers go through the following lifecycle stages:

1. **Discovery**: The system scans for provider interfaces and implementations in assemblies
2. **Installation**: Providers are registered in the system
3. **Configuration**: Provider options are configured
4. **Activation**: A specific implementation is activated for a provider type
5. **Use**: The active provider is used by the application
6. **Deactivation**: The provider is deactivated
7. **Uninstallation**: The provider is uninstalled from the system

## Key Features

1. **Multiple Implementation Support**: Each provider type can have multiple implementations
2. **Runtime Activation**: Only one implementation can be active at a time, but this can be changed at runtime
3. **Configuration Management**: Each implementation has its own configuration
4. **Health Monitoring**: Providers can report their health status
5. **Safe Assembly Loading**: Assemblies are loaded in isolated contexts for safety
6. **Dynamic Registration**: Providers are registered in the DI container dynamically
7. **API Management**: RESTful API for provider management

## Diagrams

### Component Diagram

```
┌────────────────────────┐      ┌───────────────────────────┐
│ FluentCMS Application  │◄────►│ FluentCMS.Providers.API   │
└──────────┬─────────────┘      └───────────────────────────┘
           │                                   ▲
           ▼                                   │
┌─────────────────────────┐                    │
│ FluentCMS.Providers     │                    │
│                         │←───────────────────┘
│ - ProviderManager       │
│ - AssemblyManager       │
│ - DynamicServiceProvider│
└──────────┬──────────────┘
           │
           ▼
┌─────────────────────────┐      ┌───────────────────────────┐
│FluentCMS.Providers.Data │◄────►│ Database                  │
│                         │      │                           │
│ - ProviderRepository    │      │ - Provider Types          │
│ - ProviderDbContext     │      │ - Implementations         │
└──────────┬──────────────┘      │ - Configurations          │
           │                     └───────────────────────────┘
           ▼
┌─────────────────────────┐
│FluentCMS.Providers.     │      ┌───────────────────────────┐
│  Abstractions           │◄────►│ Provider Implementations  │
│                         │      │                           │
│ - IProvider             │      │ - Email Provider          │
│ - IProviderLifecycle    │      │ - EventBus Provider       │
│ - IProviderHealth       │      │ - Storage Provider        │
└─────────────────────────┘      │ - Custom Providers        │
                                 └───────────────────────────┘
```

### Provider Activation Flow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ Controller  │────►│ Provider    │────►│ Repository  │────►│  Database   │
│             │     │ Manager     │     │             │     │             │
└─────────────┘     └──────┬──────┘     └─────────────┘     └─────────────┘
                           │
                           ▼
              ┌──────────────────────────┐
              │ 1. Deactivate current    │
              │    provider if any       │
              └──────────┬───────────────┘
                         │
                         ▼
              ┌──────────────────────────┐     ┌─────────────┐
              │ 2. Load new provider     │────►│ Assembly    │
              │    assembly              │     │ Manager     │
              └──────────┬───────────────┘     └─────────────┘
                         │
                         ▼
              ┌──────────────────────────┐     ┌─────────────┐
              │ 3. Initialize and        │────►│ Provider    │
              │    activate provider     │     │ Instance    │
              └──────────┬───────────────┘     └─────────────┘
                         │
                         ▼
              ┌──────────────────────────┐     ┌─────────────┐
              │ 4. Register provider     │────►│ Dynamic     │
              │    in DI container       │     │ Service     │
              └──────────┬───────────────┘     │ Provider    │
                         │                     └─────────────┘
                         ▼
              ┌──────────────────────────┐
              │ 5. Update database       │
              │    records               │
              └──────────────────────────┘
```

## Integration

To integrate the provider system into an application:

1. Register the provider system services in the DI container
2. Set up the provider database
3. Create provider interfaces for the required functionality
4. Create provider implementations
5. Use the provider manager to get active providers

## Documentation

For more detailed information, see the following documentation:

- [Provider Development Guide](docs/06-provider-development-guide.md)
- [Integration Guide](docs/07-integration-guide.md)

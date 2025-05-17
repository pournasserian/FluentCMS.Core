# Provider Development Guide

The FluentCMS Provider System enables developers to create pluggable, dynamically loadable components that can be swapped at runtime. This guide explains how to develop new providers and how to use the provider system effectively.

## Table of Contents

1. [Overview](#overview)
2. [Provider Architecture](#provider-architecture)
3. [Creating a New Provider](#creating-a-new-provider)
4. [Managing Providers](#managing-providers)
5. [Sample Implementation](#sample-implementation)
6. [Best Practices](#best-practices)

## Overview

The provider system is designed with the following key features:

- **Multiple provider implementations** for each provider type
- **Runtime activation/deactivation** of providers
- **Configuration management** for each provider implementation
- **Health monitoring** of active providers
- **Safe loading/unloading** of provider assemblies
- **API endpoints** for provider management

A provider is essentially a pluggable implementation of a specific functionality that can be selected and configured at runtime.

## Provider Architecture

The provider system consists of several components:

- **Provider Interfaces**: Define the functionality each provider type exposes
- **Provider Implementations**: Concrete implementations of provider interfaces
- **Provider Manager**: Manages provider loading, activation, configuration, and health checks
- **Provider Repository**: Stores provider metadata and configurations
- **Dynamic Service Provider**: Handles runtime registration of providers in the DI container

### Core Interfaces

- `IProvider`: Base interface that all providers must implement
- `IProviderLifecycle`: Interface for providers that need initialization and cleanup
- `IProviderHealth`: Interface for providers that support health checks
- `IProviderWithOptions<T>`: Interface for providers that support configuration

## Creating a New Provider

### Step 1: Define a Provider Interface

Define an interface that extends `IProvider`:

```csharp
using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers.MyFeature
{
    public interface IMyFeatureProvider : IProvider
    {
        Task DoSomethingAsync(string parameter, CancellationToken cancellationToken = default);
    }
}
```

### Step 2: Create Configuration Options (Optional)

If your provider needs configuration, create a class to hold the options:

```csharp
namespace FluentCMS.Providers.MyFeature.MyImplementation
{
    public class MyFeatureOptions
    {
        public string Setting1 { get; set; } = "DefaultValue";
        public int Setting2 { get; set; } = 100;
    }
}
```

### Step 3: Implement the Provider

Implement the provider interface, inheriting from `ProviderBase` if possible:

```csharp
using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers.MyFeature.MyImplementation
{
    public class MyFeatureProvider : ProviderBase, IMyFeatureProvider, IProviderWithOptions<MyFeatureOptions>, IProviderHealth, IProviderLifecycle
    {
        private MyFeatureOptions _options = new();
        private bool _isInitialized;
        private bool _isActive;

        // IProviderWithOptions implementation
        public void Configure(MyFeatureOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // IProviderLifecycle implementation
        public Task Initialize(CancellationToken cancellationToken = default)
        {
            _isInitialized = true;
            return Task.CompletedTask;
        }

        public Task Activate(CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
                throw new ProviderException("Provider must be initialized before activation");
            
            _isActive = true;
            return Task.CompletedTask;
        }

        public Task Deactivate(CancellationToken cancellationToken = default)
        {
            _isActive = false;
            return Task.CompletedTask;
        }

        public Task Uninstall(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        // IProviderHealth implementation
        public Task<(ProviderHealthStatus Status, string Message)> GetStatus(CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
                return Task.FromResult((ProviderHealthStatus.Warning, "Provider is not initialized"));
                
            if (!_isActive)
                return Task.FromResult((ProviderHealthStatus.Inactive, "Provider is not active"));
                
            return Task.FromResult((ProviderHealthStatus.Healthy, "Provider is healthy"));
        }

        // IMyFeatureProvider implementation
        public Task DoSomethingAsync(string parameter, CancellationToken cancellationToken = default)
        {
            EnsureActive();
            // Implementation goes here
            return Task.CompletedTask;
        }

        private void EnsureActive()
        {
            if (!_isInitialized)
                throw new ProviderException("Provider is not initialized");
                
            if (!_isActive)
                throw new ProviderException("Provider is not active");
        }
    }
}
```

### Step 4: Package the Provider

Create a separate assembly (DLL) for your provider implementation, ensuring it references the necessary provider abstractions.

## Managing Providers

### Installing Providers

Providers can be installed at runtime by uploading a provider assembly:

```csharp
// Assuming you have a file upload with the provider DLL
var implementationInfo = await _providerManager.InstallProviderAsync(filePath, cancellationToken);
```

### Activating Providers

Only one provider implementation can be active for each provider type:

```csharp
await _providerManager.SetActiveImplementationAsync(providerTypeId, implementationId, cancellationToken);
```

### Configuring Providers

Each provider implementation can have its own configuration:

```csharp
var options = new MyFeatureOptions { Setting1 = "Value", Setting2 = 200 };
await _providerManager.UpdateConfigurationAsync(implementationId, options, cancellationToken);
```

### Using Active Providers

To use the currently active provider of a specific type:

```csharp
var provider = await _providerManager.GetActiveProviderAsync<IMyFeatureProvider>(cancellationToken);
if (provider != null)
{
    await provider.DoSomethingAsync("parameter", cancellationToken);
}
```

## Sample Implementation

The FluentCMS Provider System includes a sample email provider as a reference implementation:

- `IEmailProvider`: Defines the email sending capabilities
- `SmtpEmailProvider`: Implements email sending via SMTP
- `SmtpEmailOptions`: Configuration options for the SMTP provider

## Best Practices

1. **Error Handling**: Implement proper error handling and report meaningful error messages
2. **Health Checks**: Implement the `IProviderHealth` interface to report the provider's status
3. **Lifecycle Management**: Properly handle initialization, activation, deactivation, and uninstallation
4. **Configuration Validation**: Validate configuration options during initialization
5. **Assembly Isolation**: Design providers to work in isolated assembly contexts
6. **Resource Management**: Clean up resources properly during deactivation and uninstallation
7. **Threading**: Make providers thread-safe when appropriate

By following these guidelines, you can create robust and maintainable providers that integrate seamlessly with the FluentCMS Provider System.

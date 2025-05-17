# Provider System Integration Guide

This guide explains how to integrate the FluentCMS Provider System into your application and use providers effectively.

## Table of Contents

1. [Getting Started](#getting-started)
2. [DI Setup](#di-setup)
3. [Database Setup](#database-setup)
4. [Using Providers](#using-providers)
5. [Managing Providers at Runtime](#managing-providers-at-runtime)
6. [Configuration](#configuration)
7. [Advanced Scenarios](#advanced-scenarios)

## Getting Started

To use the provider system, add references to the following packages:

- `FluentCMS.Providers.Abstractions`
- `FluentCMS.Providers`
- `FluentCMS.Providers.Data`

## DI Setup

Register the provider system services in your application startup:

```csharp
using FluentCMS.Providers;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Add provider system with SQL Server
    services.AddProviderSystem(Configuration);
    
    // Or with a custom database provider
    services.AddProviderSystem(Configuration, options => 
    {
        options.UseSqlite(Configuration.GetConnectionString("ProviderDbConnection"));
    });
}
```

## Database Setup

The provider system requires a database to store provider information. You need to ensure the database is created and migrated:

```csharp
using FluentCMS.Providers.Data;
using Microsoft.EntityFrameworkCore;

public async Task InitializeDatabase(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProviderDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

## Using Providers

### Get the Active Provider

```csharp
using FluentCMS.Providers.Abstractions;
using FluentCMS.Providers.Email;

public class EmailService
{
    private readonly IProviderManager _providerManager;
    
    public EmailService(IProviderManager providerManager)
    {
        _providerManager = providerManager;
    }
    
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var emailProvider = await _providerManager.GetActiveProviderAsync<IEmailProvider>();
        if (emailProvider == null)
        {
            throw new InvalidOperationException("No active email provider found");
        }
        
        await emailProvider.SendEmailAsync(to, subject, body);
    }
}
```

### Check Provider Availability

```csharp
public async Task<bool> IsEmailAvailableAsync()
{
    var providerType = await _providerManager.GetProviderTypesAsync()
        .FirstOrDefaultAsync(pt => pt.FullTypeName == typeof(IEmailProvider).FullName);
        
    if (providerType == null)
    {
        return false;
    }
    
    var activeImpl = await _providerManager.GetActiveImplementationAsync(providerType.Id);
    return activeImpl != null && activeImpl.HealthStatus == ProviderHealthStatus.Healthy;
}
```

## Managing Providers at Runtime

The provider system includes a controller that exposes API endpoints for provider management. To use it, register the controller in your API:

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // Maps the ProvidersController
});
```

Key API endpoints:

- `GET /api/providers/types` - Get all provider types
- `GET /api/providers/types/{typeId}/implementations` - Get implementations for a type
- `GET /api/providers/types/{typeId}/active` - Get active implementation
- `PUT /api/providers/types/{typeId}/active/{implId}` - Set active implementation
- `GET /api/providers/implementations/{implId}/configuration` - Get configuration
- `PUT /api/providers/implementations/{implId}/configuration` - Update configuration
- `POST /api/providers/install` - Upload and install a provider
- `DELETE /api/providers/implementations/{implId}` - Uninstall a provider
- `GET /api/providers/implementations/{implId}/health` - Check health
- `POST /api/providers/refresh` - Refresh provider registry

You can also create a custom UI for managing providers that uses these API endpoints.

## Configuration

Configure the provider system in your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ProviderDbConnection": "Server=(localdb)\\mssqllocaldb;Database=FluentCMS_Providers;Trusted_Connection=True;"
  },
  "ProviderSystem": {
    "ProviderDirectory": "Providers",
    "ScanAssembliesOnStartup": true
  }
}
```

## Advanced Scenarios

### Automatic Provider Discovery

To automatically discover and register providers on application startup:

```csharp
public async Task DiscoverProvidersAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var providerManager = scope.ServiceProvider.GetRequiredService<IProviderManager>();
    await providerManager.RefreshProviderRegistryAsync();
}
```

### Custom Provider Initialization

To initialize a provider manually:

```csharp
// Install the provider
var implementationInfo = await _providerManager.InstallProviderAsync("path/to/provider.dll");

// Configure the provider
var options = new SomeProviderOptions { ... };
await _providerManager.UpdateConfigurationAsync<SomeProviderOptions>(implementationInfo.Id, options);

// Activate the provider
await _providerManager.SetActiveImplementationAsync(implementationInfo.ProviderTypeId, implementationInfo.Id);
```

### Health Monitoring

To check provider health:

```csharp
public async Task<IEnumerable<ProviderHealthResult>> CheckAllProvidersHealthAsync()
{
    var results = new List<ProviderHealthResult>();
    var implementations = await _providerManager.GetImplementationsAsync();
    
    foreach (var impl in implementations)
    {
        try
        {
            var (status, message) = await _providerManager.CheckHealthAsync(impl.Id);
            results.Add(new ProviderHealthResult
            {
                ImplementationId = impl.Id,
                ProviderName = impl.Name,
                Status = status,
                Message = message,
                CheckedAt = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            results.Add(new ProviderHealthResult
            {
                ImplementationId = impl.Id,
                ProviderName = impl.Name,
                Status = ProviderHealthStatus.Unhealthy,
                Message = ex.Message,
                CheckedAt = DateTimeOffset.UtcNow
            });
        }
    }
    
    return results;
}
```

By following this integration guide, you can effectively incorporate the provider system into your FluentCMS application, allowing for dynamic management of various system components.

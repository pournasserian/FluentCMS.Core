# FluentCMS Provider System

A flexible, configuration-driven provider system for FluentCMS that enables automatic provider registration, runtime switching, and multiple provider instances with different configurations.

## Features

- **Configuration-Driven**: All providers configured through `appsettings.json`
- **No Assembly Scanning**: Explicit type loading for better performance and control
- **Multiple Instances**: Support multiple named instances of the same provider type
- **Runtime Switching**: Change active providers by updating configuration
- **Factory Pattern**: Access both active and named providers
- **Type Safety**: Strong typing for all provider interfaces
- **Hot Reload**: Optional configuration hot reload support

## Quick Start

### 1. Install the Core Package

Add reference to `FluentCMS.Providers.Core` in your project.

### 2. Configure Providers in appsettings.json

```json
{
  "Providers": {
    "Cache": "MainCache",
    "Email": "ProductionEmail"
  },
  "MainCache": {
    "ImplementationType": "FluentCMS.Providers.Caching.InMemory.InMemoryCacheProvider",
    "SizeLimit": 100,
    "CompactionPercentage": 0.25
  },
  "FastCache": {
    "ImplementationType": "FluentCMS.Providers.Caching.InMemory.InMemoryCacheProvider",
    "SizeLimit": 50
  },
  "ProductionEmail": {
    "ImplementationType": "FluentCMS.Providers.Email.Smtp.SmtpEmailProvider",
    "Host": "smtp.company.com",
    "Port": 587,
    "Username": "noreply@company.com",
    "Password": "prod-password"
  }
}
```

### 3. Register Provider System in Program.cs

```csharp
using FluentCMS.Providers.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add the provider system - this automatically registers all configured providers
builder.Services.AddProviderSystem(builder.Configuration);

var app = builder.Build();
```

### 4. Use Providers in Your Services

#### Option A: Direct Injection (Active Provider)
```csharp
public class OrderService
{
    private readonly ICacheProvider _cache;
    private readonly IEmailProvider _email;

    public OrderService(ICacheProvider cache, IEmailProvider email)
    {
        _cache = cache; // Gets "MainCache" 
        _email = email; // Gets "ProductionEmail"
    }
}
```

#### Option B: Named Provider Access
```csharp
public class NotificationService
{
    private readonly IProviderFactory<IEmailProvider> _emailFactory;

    public NotificationService(IProviderFactory<IEmailProvider> emailFactory)
    {
        _emailFactory = emailFactory;
    }

    public async Task SendWelcomeEmail(string email)
    {
        var emailProvider = _emailFactory.GetProvider("ProductionEmail");
        await emailProvider.SendAsync(email, "Welcome!", "Thank you for joining...");
    }

    public async Task SendMarketingEmail(string email)
    {
        var emailProvider = _emailFactory.GetProvider("MarketingEmail");
        await emailProvider.SendAsync(email, "Special Offer!", "Check out our deals...");
    }
}
```

## Configuration Structure

The configuration follows a simple flat structure:

```json
{
  "Providers": {
    "ProviderType": "ActiveInstanceName"
  },
  "InstanceName1": {
    "ImplementationType": "Full.Type.Name",
    "Property1": "value1",
    "Property2": "value2"
  },
  "InstanceName2": {
    "ImplementationType": "Full.Type.Name",
    "Property1": "different-value"
  }
}
```

### Configuration Sections

- **Providers**: Maps provider types to active instance names
- **Instance Configurations**: Each named instance with its implementation type and settings

## Environment-Specific Configuration

Use standard ASP.NET Core configuration patterns:

```json
// appsettings.Development.json
{
  "Providers": {
    "Cache": "FastCache",
    "Email": "DevelopmentEmail"
  }
}

// appsettings.Production.json
{
  "Providers": {
    "Cache": "ProdRedis",
    "Email": "ProductionEmail"
  }
}
```

## Advanced Configuration

### Enable Hot Reload and Health Checks

```csharp
builder.Services.AddProviderSystem(builder.Configuration, options =>
{
    options.EnableHotReload = true;
    options.EnableHealthChecks = true;
    options.ThrowOnMissingProvider = true;
});
```

### Provider Factory Methods

```csharp
public class AdvancedService
{
    private readonly IProviderFactory<ICacheProvider> _cacheFactory;

    public AdvancedService(IProviderFactory<ICacheProvider> cacheFactory)
    {
        _cacheFactory = cacheFactory;
    }

    public async Task DoWork()
    {
        // Get active provider
        var activeCache = _cacheFactory.GetActiveProvider();

        // Get specific named provider
        var fastCache = _cacheFactory.GetProvider("FastCache");

        // Get all providers
        var allCaches = _cacheFactory.GetAllProviders();

        // Check if provider exists
        if (_cacheFactory.HasProvider("RedisCache"))
        {
            var redisCache = _cacheFactory.GetProvider("RedisCache");
            // Use Redis cache
        }
    }
}
```

## Creating Custom Providers

### 1. Define Provider Interface

```csharp
public interface IStorageProvider
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName);
    Task<Stream> GetFileAsync(string fileName);
    Task DeleteFileAsync(string fileName);
}
```

### 2. Implement Provider

```csharp
public class AzureBlobStorageProvider : IStorageProvider
{
    private readonly string _connectionString;
    private readonly string _containerName;

    public AzureBlobStorageProvider(IConfiguration configuration)
    {
        // Configuration will be bound automatically
        _connectionString = configuration["ConnectionString"];
        _containerName = configuration["ContainerName"];
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
    {
        // Implementation
    }

    // Other methods...
}
```

### 3. Configure in appsettings.json

```json
{
  "Providers": {
    "Storage": "AzureBlob"
  },
  "AzureBlob": {
    "ImplementationType": "MyApp.Providers.AzureBlobStorageProvider",
    "ConnectionString": "your-azure-connection-string",
    "ContainerName": "uploads"
  }
}
```

## Runtime Provider Switching

Change the active provider by updating configuration:

```json
// Switch from InMemory to Redis cache
{
  "Providers": {
    "Cache": "RedisCache"  // Changed from "MainCache"
  }
}
```

With hot reload enabled, the change takes effect immediately without restart.

## Best Practices

1. **Use Descriptive Names**: Name provider instances clearly (e.g., "ProductionEmail", "DevelopmentCache")
2. **Environment Separation**: Use different active providers per environment
3. **Fallback Providers**: Configure backup providers for critical services
4. **Configuration Validation**: Validate provider configurations at startup
5. **Logging**: Enable logging to track provider resolution and switching

## Troubleshooting

### Common Issues

1. **Type Not Found**: Ensure `ImplementationType` is the full type name including namespace
2. **Missing Assembly**: Ensure the assembly containing the provider is referenced
3. **Configuration Binding**: Check that configuration property names match exactly
4. **Interface Naming**: Provider interfaces should end with "Provider" (e.g., `ICacheProvider`)

### Debugging

Enable detailed logging to see provider registration:

```csharp
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

## Examples

See the `Examples` folder for complete working examples demonstrating:
- Basic provider usage
- Named provider access
- Runtime switching
- Multiple provider instances
- Custom provider creation

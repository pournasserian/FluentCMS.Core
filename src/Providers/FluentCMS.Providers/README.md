# FluentCMS.Providers

A dynamic provider system for .NET 9 that enables runtime loading and management of provider implementations with built-in dependency injection support.

## Features

- **Dynamic Provider Loading**: Load provider implementations from assemblies at runtime
- **Database-Driven Configuration**: Store provider configurations in SQLite database using EF Core
- **Dependency Injection Integration**: Seamless integration with .NET's built-in DI container
- **Options Pattern Support**: Full support for `IOptionsMonitor<T>` pattern
- **Runtime Management**: Add, update, activate, and deactivate providers at runtime
- **Area-Based Organization**: Organize providers by functional areas (Email, VirtualFile, etc.)
- **Singleton Provider Lifecycle**: Efficient singleton pattern with thread-safe operations
- **Assembly Security**: Only loads assemblies matching `FluentCMS.Providers.*.dll` pattern

## Installation

```bash
dotnet add package FluentCMS.Providers
```

## Quick Start

### 1. Add to your Program.cs

```csharp
using FluentCMS.Providers.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add the provider system
builder.Services.AddProviderSystem("Data Source=providers.db");

// Or with automatic discovery during startup
builder.Services.AddProviderSystemWithDiscovery("Data Source=providers.db");

var app = builder.Build();

// Initialize the provider system (if not using AddProviderSystemWithDiscovery)
await app.Services.InitializeProviderSystem();
```

### 2. Define Area-Specific Interfaces

```csharp
using FluentCMS.Providers.Abstractions;

public interface IEmailProvider : IProvider
{
    Task SendEmail(string to, string subject, string body);
    bool IsValidEmail(string email);
}
```

### 3. Create Provider Implementation

```csharp
// In your provider assembly: FluentCMS.Providers.Email.dll
using FluentCMS.Providers.Abstractions;
using Microsoft.Extensions.Options;

public class SmtpEmailProvider : IEmailProvider
{
    private readonly IOptionsMonitor<SmtpOptions> _options;
    
    public SmtpEmailProvider(IOptionsMonitor<SmtpOptions> options)
    {
        _options = options;
    }
    
    public async Task SendEmail(string to, string subject, string body)
    {
        var config = _options.CurrentValue;
        // Implementation using config.Host, config.Port, etc.
    }
    
    public bool IsValidEmail(string email)
    {
        return email.Contains("@");
    }
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### 4. Create Provider Module

```csharp
using FluentCMS.Providers.Core;
using Microsoft.Extensions.DependencyInjection;

public class SmtpEmailProviderModule : ProviderModuleBase<SmtpEmailProvider, SmtpOptions>
{
    public override string Area => "Email";
    public override string DisplayName => "SMTP Email Provider";
    
    public override void ConfigureServices(IServiceCollection services, string providerName)
    {
        // Register any additional services needed by this provider
        services.AddHttpClient($"SmtpClient_{providerName}");
    }
}
```

### 5. Use in Your Services

```csharp
public class NotificationService
{
    private readonly IEmailProvider _emailProvider;
    
    public NotificationService(IEmailProvider emailProvider)
    {
        _emailProvider = emailProvider;
    }
    
    public async Task SendWelcomeEmail(string userEmail)
    {
        await _emailProvider.SendEmail(userEmail, "Welcome!", "Welcome to our service!");
    }
}

// Register the active provider for dependency injection
builder.Services.AddAreaProvider<IEmailProvider>("Email");
```

## Advanced Usage

### Managing Providers Programmatically

```csharp
public class ProviderManagementService
{
    private readonly IProviderService _providerService;
    private readonly IProviderFactory _providerFactory;
    
    public ProviderManagementService(IProviderService providerService, IProviderFactory providerFactory)
    {
        _providerService = providerService;
        _providerFactory = providerFactory;
    }
    
    public async Task RegisterNewProvider()
    {
        var configuration = """
        {
            "Host": "smtp.gmail.com",
            "Port": 587,
            "Username": "user@gmail.com",
            "Password": "password"
        }
        """;
        
        await _providerService.RegisterProvider(
            "Gmail", 
            "Email", 
            "FluentCMS.Providers.Email.dll", 
            "SmtpEmailProvider", 
            configuration, 
            isActive: true);
    }
    
    public async Task SwitchProvider()
    {
        await _providerService.ActivateProvider("Email", "Outlook");
    }
    
    public async Task UseSpecificProvider()
    {
        var gmailProvider = await _providerFactory.GetProvider<IEmailProvider>("Email", "Gmail");
        await gmailProvider?.SendEmail("test@example.com", "Test", "Test message");
    }
}
```

### Multiple Provider Instances

```csharp
// Register multiple SMTP providers with different configurations
await providerService.RegisterProvider("Gmail", "Email", "FluentCMS.Providers.Email.dll", "SmtpEmailProvider", gmailConfig);
await providerService.RegisterProvider("Outlook", "Email", "FluentCMS.Providers.Email.dll", "SmtpEmailProvider", outlookConfig);

// Only one can be active at a time
await providerService.ActivateProvider("Email", "Gmail"); // Outlook becomes inactive
```

## Architecture

### Core Components

- **IProvider**: Base marker interface for all providers
- **IProviderModule**: Defines how providers are configured and created
- **IProviderService**: Manages provider CRUD operations and lifecycle
- **IProviderFactory**: Creates and caches provider instances
- **IProviderAssemblyLoader**: Securely loads and scans provider assemblies
- **ProviderDbContext**: EF Core context for provider storage

### Database Schema

**Providers Table:**
- Id, Name, Area, AssemblyPath, TypeName, IsActive, Configuration, CreatedAt, UpdatedAt

**ProviderAreas Table:**
- Name, Description, MaxActiveProviders

## Best Practices

1. **One Active Provider Per Area**: Only one provider should be active per area at any time
2. **Unique Names**: Provider names must be unique within their area
3. **Thread Safety**: Providers must be thread-safe (singleton lifecycle)
4. **Assembly Naming**: Provider assemblies must follow `FluentCMS.Providers.*.dll` pattern
5. **Options Validation**: Implement validation in your options classes
6. **Error Handling**: Providers should handle errors gracefully
7. **Resource Cleanup**: Implement `IDisposable` if your provider holds resources

## Security

- Only assemblies matching `FluentCMS.Providers.*.dll` are loaded
- Assembly loading is restricted to the application's base directory
- Provider modules are validated before instantiation

## Logging

The provider system includes comprehensive logging:
- Provider lifecycle events (loading, activation, deactivation)
- Assembly scanning and loading operations
- Configuration changes and validation
- Error and warning logs for troubleshooting

## Contributing

1. Follow the established patterns for provider implementations
2. Ensure thread safety in all provider implementations
3. Include comprehensive tests for new functionality
4. Update documentation for new features

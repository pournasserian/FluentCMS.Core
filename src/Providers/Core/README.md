# FluentCMS Core Provider System

A comprehensive, pluggable provider system for FluentCMS that enables modular architecture through discoverable, configurable, and swappable service implementations.

## Overview

The FluentCMS Core Provider System is designed to support multiple implementations of the same functionality while maintaining clean separation of concerns, thread safety, and high performance. This system allows you to plug in different providers for services like email, file storage, caching, logging, and more, with only one provider active per functional area at any given time.

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
├──────────────────────────────────────────────────────────────┤
│  IEmailProvider  │ IStorageProvider │ ICacheProvider │ ...   │
├──────────────────────────────────────────────────────────────┤
│                   FluentCMS.Providers                        │
│  ┌─────────────────┐ ┌─────────────────┐ ┌──────────────────┐│
│  │ ProviderManager │ │ Provider        │ │ Provider         ││
│  │                 │ │ Discovery       │ │ Resolution       ││
│  └─────────────────┘ └─────────────────┘ └──────────────────┘│
├──────────────────────────────────────────────────────────────┤
│              FluentCMS.Providers.Abstractions                │
│    IProvider │ IProviderModule │ ProviderModuleBase          │
├──────────────────────────────────────────────────────────────┤
│                   Repository Layer                           │
│  Configuration    │           Entity Framework               │
│  Repository       │           Repository                     │
└──────────────────────────────────────────────────────────────┘
```

## Key Features

- **🔌 Pluggable Architecture**: Easy provider swapping without code changes
- **🔍 Auto-Discovery**: Automatic provider module detection via assembly scanning
- **⚡ High Performance**: Optimized caching, lazy loading, and thread-safe operations
- **🛡️ Thread-Safe**: Concurrent access with proper synchronization primitives
- **📊 Configuration Flexible**: Support for both database and configuration file-based management
- **🎯 Dependency Injection**: Full ASP.NET Core DI container integration
- **📈 Performance Monitoring**: Built-in caching and performance optimizations
- **🔒 Data Integrity**: Database constraints ensuring single active provider per area

## Projects

| Project | Description | Documentation |
|---------|-------------|---------------|
| **FluentCMS.Providers.Abstractions** | Core interfaces and base classes for provider implementations | [📖 Docs](FluentCMS.Providers.Abstractions/README.md) |
| **FluentCMS.Providers** | Main provider system implementation with discovery, caching, and management | [📖 Docs](FluentCMS.Providers/README.md) |
| **FluentCMS.Providers.Repositories.EntityFramework** | Entity Framework repository implementation for database-backed provider storage | [📖 Docs](FluentCMS.Providers.Repositories.EntityFramework/README.md) |

## Quick Start

### 1. Installation

```bash
# Core provider system
dotnet add package FluentCMS.Providers

# Provider abstractions (if creating custom providers)
dotnet add package FluentCMS.Providers.Abstractions

# Entity Framework repository (for database storage)
dotnet add package FluentCMS.Providers.Repositories.EntityFramework
```

### 2. Basic Setup

```csharp
// Program.cs
using FluentCMS.Providers;

var builder = WebApplication.CreateBuilder(args);

// Register the provider system
builder.Services.AddProviders(options =>
{
    options.AssemblyPrefixesToScan.Add("FluentCMS");
    options.AssemblyPrefixesToScan.Add("MyCompany");
    options.EnableLogging = true;
});

// Choose repository implementation
builder.Services.AddProviders()
                .UseConfiguration(); // Configuration-based

// OR for database storage
builder.Services.AddDbContext<ProviderDbContext>(options =>
    options.UseSqlServer(connectionString));
    
builder.Services.AddProviders()
                .UseEntityFramework(); // Database-based

var app = builder.Build();
```

### 3. Configuration

**appsettings.json:**
```json
{
  "Providers": {
    "Email": {
      "SmtpProvider": {
        "Name": "SmtpProvider",
        "Active": true,
        "Module": "MyCompany.Email.SmtpProviderModule",
        "Options": {
          "SmtpServer": "smtp.gmail.com",
          "Port": 587,
          "UseSsl": true,
          "Username": "your-email@gmail.com",
          "Password": "your-password"
        }
      }
    },
    "Storage": {
      "LocalFileProvider": {
        "Name": "LocalFileProvider",
        "Active": true,
        "Module": "MyCompany.Storage.LocalFileProviderModule",
        "Options": {
          "BasePath": "/var/storage",
          "MaxFileSize": 10485760
        }
      }
    }
  }
}
```

### 4. Creating a Custom Provider

**Step 1: Define the Interface**
```csharp
public interface IEmailProvider : IProvider
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task<bool> ValidateEmailAsync(string email, CancellationToken cancellationToken = default);
}
```

**Step 2: Create Options Class**
```csharp
public class SmtpOptions
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

**Step 3: Implement the Provider**
```csharp
public class SmtpProvider : IEmailProvider
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpProvider> _logger;

    public SmtpProvider(SmtpOptions options, ILogger<SmtpProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.SmtpServer, _options.Port);
        client.EnableSsl = _options.UseSsl;
        client.Credentials = new NetworkCredential(_options.Username, _options.Password);

        var message = new MailMessage(_options.Username, to, subject, body);
        await client.SendMailAsync(message, cancellationToken);
        
        _logger.LogInformation("Email sent successfully to {Recipient}", to);
    }

    public async Task<bool> ValidateEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return MailAddress.TryCreate(email, out _);
    }
}
```

**Step 4: Create Provider Module**
```csharp
public class SmtpProviderModule : ProviderModuleBase<SmtpProvider, SmtpOptions>
{
    public override string Area => "Email";
    public override string DisplayName => "SMTP Email Provider";

    public override void ConfigureServices(IServiceCollection services)
    {
        // Register additional dependencies if needed
        services.AddTransient<IEmailValidator, EmailValidator>();
    }
}
```

### 5. Using Providers

```csharp
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly IEmailProvider _emailProvider;

    public NotificationController(IEmailProvider emailProvider)
    {
        _emailProvider = emailProvider;
    }

    [HttpPost("send-welcome")]
    public async Task<IActionResult> SendWelcomeEmail([FromBody] WelcomeEmailRequest request)
    {
        try
        {
            await _emailProvider.SendEmailAsync(
                request.Email, 
                "Welcome to FluentCMS!", 
                "Thank you for joining our platform!");
                
            return Ok("Email sent successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to send email: {ex.Message}");
        }
    }
}
```

## Advanced Usage

### Dynamic Provider Switching

```csharp
public class ProviderSwitchingService
{
    private readonly IProviderManager _providerManager;
    private readonly IProviderRepository _repository;

    public ProviderSwitchingService(IProviderManager providerManager, IProviderRepository repository)
    {
        _providerManager = providerManager;
        _repository = repository;
    }

    public async Task SwitchEmailProvider(string newProviderName)
    {
        // Deactivate current email providers
        var emailProviders = await _repository.GetByArea("Email");
        foreach (var provider in emailProviders.Where(p => p.IsActive))
        {
            provider.IsActive = false;
            await _repository.Update(provider);
        }

        // Activate new provider
        var newProvider = await _repository.GetByAreaAndName("Email", newProviderName);
        if (newProvider != null)
        {
            newProvider.IsActive = true;
            await _repository.Update(newProvider);
        }

        // Refresh provider cache
        await _providerManager.RefreshProviders();
    }
}
```

### Provider Health Monitoring

```csharp
public class ProviderHealthCheckService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProviderHealthCheckService> _logger;

    public async Task<HealthCheckResult> CheckEmailProviderHealth()
    {
        try
        {
            var emailProvider = _serviceProvider.GetRequiredService<IEmailProvider>();
            var isValid = await emailProvider.ValidateEmailAsync("test@example.com");
            
            return isValid 
                ? HealthCheckResult.Healthy("Email provider is operational")
                : HealthCheckResult.Degraded("Email provider validation failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email provider health check failed");
            return HealthCheckResult.Unhealthy("Email provider is not available", ex);
        }
    }
}
```

### Multi-Environment Configuration

```csharp
// Program.cs
builder.Services.AddProviders(options =>
{
    options.AssemblyPrefixesToScan.Add("FluentCMS");
    options.AssemblyPrefixesToScan.Add("MyCompany");
    
    // Environment-specific settings
    options.EnableLogging = builder.Environment.IsDevelopment();
    options.IgnoreExceptions = builder.Environment.IsProduction();
});

// Environment-specific provider registration
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddProviders().UseConfiguration();
}
else
{
    builder.Services.AddProviders().UseEntityFramework();
}
```

## Performance Characteristics

| Operation | Performance | Notes |
|-----------|-------------|-------|
| Provider Resolution | O(1) | Cached lookup after initialization |
| Provider Discovery | O(n) | One-time cost during startup |
| Provider Switching | O(k) | Where k = providers in area |
| Configuration Loading | O(m) | Where m = configuration entries |

## Troubleshooting

### Common Issues

**Provider Not Found**
```
InvalidOperationException: No active provider found for area 'Email'
```
- Verify provider is marked as Active in configuration/database
- Check assembly prefix includes provider assembly
- Ensure provider module has parameterless constructor

**Multiple Active Providers**
```
InvalidOperationException: Multiple active providers found for area 'Email'
```
- Only one provider per area can be active
- Check configuration for duplicate Active: true entries
- Verify database constraints are properly enforced

**Assembly Loading Issues**
```
ReflectionTypeLoadException when scanning assemblies
```
- Ensure all dependencies are available
- Check .NET version compatibility
- Consider setting IgnoreExceptions: true for problematic assemblies

### Debugging Tips

1. **Enable Logging**: Set `EnableLogging = true` in provider options
2. **Check Assembly Loading**: Monitor which assemblies are being scanned
3. **Validate Configuration**: Ensure JSON configuration is valid
4. **Database Constraints**: Verify database schema is up to date
5. **Dependency Injection**: Check service registration order

## Performance Best Practices

1. **Assembly Scanning**: Limit `AssemblyPrefixesToScan` to necessary prefixes
2. **Caching**: Provider resolution is cached - avoid frequent refreshes
3. **Database Operations**: Use `AsNoTracking()` for read-only operations
4. **Error Handling**: Implement circuit breaker patterns for external providers
5. **Memory Management**: Clear discovery caches in low-memory scenarios

## Contributing

1. **Fork** the repository
2. **Create** a feature branch
3. **Add** comprehensive tests
4. **Update** documentation
5. **Submit** a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📚 **Documentation**: Browse the individual project README files
- 🐛 **Issues**: Report bugs or request features on GitHub
- 💬 **Discussions**: Join the community discussions
- 📧 **Contact**: Reach out to the maintainers

---

*Built with ❤️ for the FluentCMS ecosystem*

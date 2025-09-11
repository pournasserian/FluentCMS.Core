using FluentCMS.Providers.Caching.Abstractions;
using FluentCMS.Providers.EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Providers.Core.Examples;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .Build();

        // Build host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add the provider system - this will automatically register all providers
                services.AddProviderSystem(configuration, options =>
                {
                    options.EnableHotReload = true;
                    options.EnableHealthChecks = true;
                    options.ThrowOnMissingProvider = true;
                });

                // Add application services
                services.AddScoped<ExampleService>();
            })
            .Build();

        // Run example
        var exampleService = host.Services.GetRequiredService<ExampleService>();
        await exampleService.RunExamples();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

/// <summary>
/// Example service demonstrating provider usage
/// </summary>
public class ExampleService
{
    private readonly ICacheProvider _cache;
    private readonly IEventPublisher _eventPublisher;
    private readonly IProviderFactory<ICacheProvider> _cacheFactory;
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(
        ICacheProvider cache,
        IEventPublisher eventPublisher,
        IProviderFactory<ICacheProvider> cacheFactory,
        ILogger<ExampleService> logger)
    {
        _cache = cache;
        _eventPublisher = eventPublisher;
        _cacheFactory = cacheFactory;
        _logger = logger;
    }

    public async Task RunExamples()
    {
        _logger.LogInformation("Running provider system examples...");

        // Example 1: Using active providers (injected directly)
        await UseActiveProviders();

        // Example 2: Using named providers via factory
        await UseNamedProviders();

        // Example 3: Switching between providers
        await SwitchBetweenProviders();

        _logger.LogInformation("All examples completed successfully!");
    }

    private async Task UseActiveProviders()
    {
        _logger.LogInformation("=== Example 1: Using Active Providers ===");

        // Use the active cache provider (MainCache)
        await _cache.Set("example-key", "Hello from active cache provider!", TimeSpan.FromMinutes(5));
        var cachedValue = await _cache.Get<string>("example-key");
        _logger.LogInformation("Cached value: {Value}", cachedValue);

        // Use the active event publisher
        await _eventPublisher.Publish(new ExampleEvent { Message = "Hello from active event publisher!" });
        _logger.LogInformation("Event published successfully");
    }

    private async Task UseNamedProviders()
    {
        _logger.LogInformation("=== Example 2: Using Named Providers ===");

        // Use specific cache providers by name
        var mainCache = _cacheFactory.GetProvider("MainCache");
        var fastCache = _cacheFactory.GetProvider("FastCache");

        await mainCache.Set("main-cache-key", "Data in main cache", TimeSpan.FromMinutes(10));
        await fastCache.Set("fast-cache-key", "Data in fast cache", TimeSpan.FromMinutes(2));

        var mainValue = await mainCache.Get<string>("main-cache-key");
        var fastValue = await fastCache.Get<string>("fast-cache-key");

        _logger.LogInformation("Main cache value: {Value}", mainValue);
        _logger.LogInformation("Fast cache value: {Value}", fastValue);
    }

    private async Task SwitchBetweenProviders()
    {
        _logger.LogInformation("=== Example 3: Provider Information ===");

        // Show all available cache providers
        var allCacheProviders = _cacheFactory.GetAllProviders();
        _logger.LogInformation("Available cache providers: {Providers}", string.Join(", ", allCacheProviders.Keys));

        // Check if specific providers exist
        var hasMainCache = _cacheFactory.HasProvider("MainCache");
        var hasFastCache = _cacheFactory.HasProvider("FastCache");
        var hasRedisCache = _cacheFactory.HasProvider("RedisCache");

        _logger.LogInformation("Has MainCache: {HasProvider}", hasMainCache);
        _logger.LogInformation("Has FastCache: {HasProvider}", hasFastCache);
        _logger.LogInformation("Has RedisCache: {HasProvider}", hasRedisCache);

        // Demonstrate that changing configuration would switch active provider
        _logger.LogInformation("To switch active cache provider, change 'Providers:Cache' in appsettings.json");
        _logger.LogInformation("Current active provider is determined by configuration");
    }
}

/// <summary>
/// Example event for testing
/// </summary>
public class ExampleEvent : IEvent
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

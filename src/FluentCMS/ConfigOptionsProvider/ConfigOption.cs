using FluentCMS.Repositories.EntityFramework;
using FluentCMS.TodoApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace FluentCMS.ConfigOptionsProvider;

public class ConfigOption : AuditableEntity
{
    public string TypeName { get; set; } = default!;
    public string Value { get; set; } = default!;
}

// DbContext for accessing the configuration database
public class ConfigDbContext(DbContextOptions<ConfigDbContext> options) : BaseDbContext(options)
{
    public DbSet<ConfigOption> ConfigOptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigOption>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<ConfigOption>()
            .Property(t => t.TypeName)
            .IsRequired();

        modelBuilder.Entity<ConfigOption>()
            .Property(t => t.Value)
            .IsRequired();
    }
}

// Registry that keeps track of registered option types
public class OptionsRegistry
{
    private readonly Dictionary<Type, string> _registeredOptions = [];

    // Register an options type with the registry
    public void RegisterOptions<T>(string? configSectionPath = null) where T : class, new()
    {
        var type = typeof(T);

        // Use type name as section path if none provided
        if (string.IsNullOrEmpty(configSectionPath))
        {
            configSectionPath = type.Name;
        }

        // Register or update the options type
        if (!_registeredOptions.TryAdd(type, configSectionPath))
        {
            _registeredOptions[type] = configSectionPath;
        }
    }

    // Get all registered options types
    public IReadOnlyDictionary<Type, string> GetRegisteredOptions()
    {
        return _registeredOptions;
    }

    // Check if a type is registered
    public bool IsRegistered<T>() where T : class, new()
    {
        return _registeredOptions.ContainsKey(typeof(T));
    }

    // Get the section path for a type
    public string GetSectionPath<T>() where T : class, new()
    {
        return _registeredOptions.TryGetValue(typeof(T), out var path) ? path : null;
    }
}

public class OptionsInitializer(ConfigDbContext dbContext, IConfiguration configuration, OptionsRegistry optionsRegistry)
{

    // Initialize all registered options that don't exist in the database
    public async Task InitializeOptionsAsync()
    {
        // Get all registered option types
        var registeredOptions = optionsRegistry.GetRegisteredOptions();

        // Get all existing options in the database
        var existingOptions = await dbContext.ConfigOptions.ToListAsync();
        var existingTypeNames = existingOptions.Select(o => o.TypeName).ToHashSet();

        // Prepare serializer options
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
        };

        // Check each registered option
        var optionsToAdd = new List<ConfigOption>();

        foreach (var optionEntry in registeredOptions)
        {
            var optionType = optionEntry.Key;
            var sectionPath = optionEntry.Value;
            var typeName = optionType.FullName;

            // Skip if already exists in database
            if (existingTypeNames.Contains(typeName))
            {
                continue;
            }

            // Try to get from appsettings.json
            object optionInstance = null;

            if (!string.IsNullOrEmpty(sectionPath))
            {
                // Get configuration section
                var section = configuration.GetSection(sectionPath);

                if (section.Exists())
                {
                    // Use reflection to call the generic Get method
                    var method = typeof(ConfigurationBinder)
                        .GetMethod(nameof(ConfigurationBinder.Get), new[] { typeof(IConfiguration), typeof(Action<BinderOptions>) })
                        .MakeGenericMethod(optionType);

                    optionInstance = method.Invoke(null, new object[]
                    {
                            section,
                            new Action<BinderOptions>(options => options.BindNonPublicProperties = true)
                    });
                }
            }

            // If not found in appsettings, create default instance
            if (optionInstance == null)
            {
                optionInstance = Activator.CreateInstance(optionType);
            }

            // Serialize and prepare to add to database
            var serializedValue = JsonSerializer.Serialize(optionInstance, jsonOptions);

            optionsToAdd.Add(new ConfigOption
            {
                TypeName = typeName,
                Value = serializedValue
            });
        }

        // Add all missing options to the database
        if (optionsToAdd.Any())
        {
            await dbContext.ConfigOptions.AddRangeAsync(optionsToAdd);
            await dbContext.SaveChangesAsync();
        }
    }
}

// Custom configuration provider that reads from the database
public class DatabaseConfigurationProvider(ConfigDbContext dbContext) : ConfigurationProvider
{

    // Load configuration from database
    public override void Load()
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Get all options from database
        var options = dbContext.ConfigOptions.ToList();

        foreach (var option in options)
        {
            // Just use the type name as the key
            data[option.TypeName] = option.Value;
        }

        Data = data;
    }

    // Method to reload configuration (will be called when options change)
    public void Reload()
    {
        Load();
        OnReload();
    }
}

// Configuration source for the database provider
public class DatabaseConfigurationSource(ConfigDbContext dbContext) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DatabaseConfigurationProvider(dbContext);
    }
}

// Configuration change token provider for options
public class ConfigurationChangeTokenProvider<T>(DatabaseConfigurationProvider provider) : IOptionsChangeTokenSource<T>
{
    public string Name => typeof(DatabaseConfigurationProvider).FullName;

    public IChangeToken GetChangeToken()
    {
        return new ConfigurationChangeToken(provider);
    }
}

// Custom change token for configuration changes
public class ConfigurationChangeToken(DatabaseConfigurationProvider provider) : IChangeToken
{
    private static readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

    public bool HasChanged => false;

    public bool ActiveChangeCallbacks => true;

    public IDisposable RegisterChangeCallback(Action<object> callback, object state)
    {
        return new ChangeTokenRegistration(_tokenSource.Token, callback, state);
    }

    // Helper to trigger configuration reload
    public static void OnChange()
    {
        var oldTokenSource = Interlocked.Exchange(
            ref _tokenSource,
            new CancellationTokenSource());

        oldTokenSource.Cancel();
    }

    private class ChangeTokenRegistration : IDisposable
    {
        private readonly CancellationTokenRegistration _registration;

        public ChangeTokenRegistration(
            CancellationToken token,
            Action<object> callback,
            object state)
        {
            _registration = token.Register(callback, state);
        }

        public void Dispose()
        {
            _registration.Dispose();
        }
    }
}

public class ConfigurationService(ConfigDbContext dbContext, DatabaseConfigurationProvider configProvider, OptionsRegistry optionsRegistry)
{

    // Get configuration as a typed object
    public async Task<T> GetConfigurationAsync<T>() where T : class, new()
    {
        // Ensure the type is registered
        if (!optionsRegistry.IsRegistered<T>())
        {
            throw new InvalidOperationException($"The type {typeof(T).FullName} is not registered with the options registry.");
        }

        var typeName = typeof(T).FullName;

        var config = await dbContext.ConfigOptions
            .FirstOrDefaultAsync(o => o.TypeName == typeName);

        if (config == null)
            return new T();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
        };

        return JsonSerializer.Deserialize<T>(config.Value, jsonOptions);
    }

    // Update configuration
    public async Task UpdateConfigurationAsync<T>(T options) where T : class, new()
    {
        // Ensure the type is registered
        if (!optionsRegistry.IsRegistered<T>())
        {
            throw new InvalidOperationException($"The type {typeof(T).FullName} is not registered with the options registry.");
        }

        var typeName = typeof(T).FullName;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
        };

        var serializedValue = JsonSerializer.Serialize(options, jsonOptions);

        var config = await dbContext.ConfigOptions
            .FirstOrDefaultAsync(o => o.TypeName == typeName);

        if (config == null)
        {
            config = new ConfigOption
            {
                TypeName = typeName,
                Value = serializedValue,
                LastUpdated = DateTime.UtcNow
            };

            dbContext.ConfigOptions.Add(config);
        }
        else
        {
            config.Value = serializedValue;
            config.LastUpdated = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        // Reload configuration to reflect changes
        configProvider.Reload();
        ConfigurationChangeToken.OnChange();
    }

    // Delete configuration
    public async Task DeleteConfigurationAsync<T>() where T : class, new()
    {
        var typeName = typeof(T).FullName;

        var config = await dbContext.ConfigOptions
            .FirstOrDefaultAsync(o => o.TypeName == typeName);

        if (config != null)
        {
            dbContext.ConfigOptions.Remove(config);
            await dbContext.SaveChangesAsync();

            // Reload configuration to reflect changes
            configProvider.Reload();
            ConfigurationChangeToken.OnChange();
        }
    }

    // Get all configuration options
    public async Task<ConfigOption[]> GetAllConfigurationsAsync()
    {
        return await dbContext.ConfigOptions.ToArrayAsync();
    }
}

// Hosted service that automatically initializes registered options during startup
internal class OptionsInitializationHostedService(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Create a scope to resolve scoped services
        using var scope = serviceProvider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<OptionsInitializer>();

        // Initialize all registered options
        await initializer.InitializeOptionsAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Nothing to do on shutdown
        return Task.CompletedTask;
    }
}

public static class DynamicConfigurationExtensions
{
    // Add the dynamic configuration infrastructure
    public static IServiceCollection AddDynamicConfiguration(this IServiceCollection services, Action<DbContextOptionsBuilder> dbContextOptions)
    {
        // Register DbContext
        services.AddDbContext<ConfigDbContext>(dbContextOptions);

        // Register the options registry as a singleton
        services.AddSingleton<OptionsRegistry>();

        // Create database and get database context
        var serviceProvider = services.BuildServiceProvider();
        using (var scope = serviceProvider.CreateScope())
        {
            var _dbContext = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
            _dbContext.Database.EnsureCreated();
        }

        // Set up the configuration provider
        var dbContext = serviceProvider.GetRequiredService<ConfigDbContext>();
        var configSource = new DatabaseConfigurationSource(dbContext);
        var configProvider = (DatabaseConfigurationProvider)configSource.Build(null);

        // Load initial configuration
        configProvider.Load();

        // Register the configuration provider as a singleton
        services.AddSingleton(configProvider);

        // Register the options initializer
        services.AddScoped<OptionsInitializer>();

        // Register the configuration service
        services.AddScoped<ConfigurationService>();

        // Register the automatic initialization hosted service
        services.AddHostedService<OptionsInitializationHostedService>();

        return services;
    }

    // Register options with the registry and set up for change notification
    public static IServiceCollection AddDynamicOptions<TOptions>(
        this IServiceCollection services,
        string? configSectionPath = null)
        where TOptions : class, new()
    {
        // Get the options registry
        var serviceProvider = services.BuildServiceProvider();
        var optionsRegistry = serviceProvider.GetRequiredService<OptionsRegistry>();

        // Register the options type with the registry
        optionsRegistry.RegisterOptions<TOptions>(configSectionPath);

        // Get the configuration provider
        var configProvider = serviceProvider.GetRequiredService<DatabaseConfigurationProvider>();

        // Configure the options with the IOptionsMonitor pattern
        services.AddOptions<TOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                // Try to get strongly typed options from the configuration
                var typeName = typeof(TOptions).FullName;

                if (config[typeName] is string json && !string.IsNullOrEmpty(json))
                {
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                    };

                    // Deserialize from the database value
                    var deserializedOptions = System.Text.Json.JsonSerializer
                        .Deserialize<TOptions>(json, jsonOptions);

                    if (deserializedOptions != null)
                    {
                        // Copy all properties to the target options instance
                        foreach (var prop in typeof(TOptions).GetProperties())
                        {
                            if (prop.CanWrite)
                            {
                                prop.SetValue(options, prop.GetValue(deserializedOptions));
                            }
                        }
                    }
                }
            })
            .Services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(
                new ConfigurationChangeTokenProvider<TOptions>(configProvider));

        return services;
    }
}
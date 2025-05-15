using FluentCMS.Repositories.EntityFramework;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FluentCMS.DynamicOptionsProvider;

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
            .ToTable("ConfigOptions");

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


// Custom configuration provider that reads from the database
public class DatabaseConfigurationProvider(ConfigDbContext dbContext) : ConfigurationProvider
{
    // Load configuration from database
    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

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

public static class DynamicConfigurationExtensions
{
    // Initialize all registered options that don't exist in the database
    public static async Task InitializeDynamicOptions(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();

        try
        {
            // Add this check to avoid conflicts
            if (!dbContext.Database.CanConnect())
            {
                dbContext.Database.EnsureCreated();
            }
            else
            {
                // For subsequent DbContexts, we need a different approach
                // This will ensure the tables for this specific DbContext are created
                // without dropping existing tables
                var script = dbContext.Database.GenerateCreateScript();
                dbContext.Database.ExecuteSqlRaw(script);
            }
        }
        catch (Exception)
        {
        }
        

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

        foreach (var optionEntry in _pendingConfigurations)
        {
            var optionType = optionEntry.Key;
            var typeName = optionType.Name;

            // Skip if already exists in database
            if (existingTypeNames.Contains(typeName))
            {
                continue;
            }

            object optionInstance = optionEntry.Value;

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

    // Add the dynamic configuration infrastructure
    public static void AddDynamicConfiguration(this IHostApplicationBuilder builder)
    {
        var configBuilder = builder.Configuration as IConfigurationBuilder;
        configBuilder.Add(new DatabaseConfigurationSource(connectionString));

        var services = builder.Services;

        // Register DbContext
        services.AddCoreDbContext<ConfigDbContext>();

        // Register the configuration provider as a singleton
        services.AddSingleton<DatabaseConfigurationProvider>();

    }

    private static readonly Dictionary<Type, object> _pendingConfigurations = [];

    private static void AddDynamicOptionsInstace<TOptions>(this IServiceCollection services, Action<TOptions>? action = default) where TOptions : class, new()
    {
        var optionInstance = new TOptions();
        action?.Invoke(optionInstance);
        var type = typeof(TOptions);

        // Queue the configuration action to be applied when the registry is created
        if (!_pendingConfigurations.ContainsKey(type))
        {
            _pendingConfigurations[type] = optionInstance;
        }
    }

    // Register options with the registry and set up for change notification
    public static IServiceCollection AddDynamicOptions<TOptions>(this IServiceCollection services, Action<TOptions>? action = default) where TOptions : class, new()
    {
        // Register the options instance
        services.AddDynamicOptionsInstace(action);

        // Configure the options with the IOptionsMonitor pattern
        services.AddOptions<TOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                // Try to get strongly typed options from the configuration
                var typeName = typeof(TOptions).Name;

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
            });

        return services;
    }
}
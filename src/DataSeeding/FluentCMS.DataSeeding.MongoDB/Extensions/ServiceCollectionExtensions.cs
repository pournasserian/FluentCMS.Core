using System;
using System.Linq;
using FluentCMS.DataSeeding.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.DataSeeding.MongoDB.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register MongoDB data seeding services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MongoDB data seeding services to the dependency injection container.
    /// This is the main entry point for configuring database seeding.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="databaseName">MongoDB database name</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddMongoDbDataSeeding("mongodb://localhost:27017", "MyAppDb", options =>
    /// {
    ///     options.AssemblySearchPatterns.Add("MyApp.*.dll");
    ///     options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMongoDbDataSeeding(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        Action<MongoDbDataSeedingOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
            
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));

        // Create and configure options
        var options = new MongoDbDataSeedingOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        };

        // Apply default patterns if none specified
        if (options.AssemblySearchPatterns.Count == 0)
        {
            // Default to scanning the entry assembly and common patterns
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var assemblyName = entryAssembly.GetName().Name;
                options.AssemblySearchPatterns.Add($"{assemblyName}.dll");
                options.AssemblySearchPatterns.Add($"{assemblyName}.*.dll");
            }
            
            // Add some common fallback patterns
            options.AssemblySearchPatterns.Add("*.dll");
        }

        // Apply user configuration
        configure?.Invoke(options);

        // Validate configuration
        var validationErrors = options.Validate();
        if (validationErrors.Any())
        {
            throw new InvalidOperationException($"Invalid MongoDB data seeding configuration: {string.Join("; ", validationErrors)}");
        }

        // Register options as singleton
        services.AddSingleton(options);

        // Register the seeding engine
        services.AddScoped<Engine.MongoDbDataSeedingEngine>();

        // Register the hosted service that will execute seeding
        services.AddHostedService<DataSeedingHostedService>();

        return services;
    }

    /// <summary>
    /// Adds MongoDB data seeding services with development environment condition.
    /// This is a convenience method that automatically adds a condition to only run in Development.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="databaseName">MongoDB database name</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMongoDbDataSeedingForDevelopment(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        Action<MongoDbDataSeedingOptions>? configure = null)
    {
        return services.AddMongoDbDataSeeding(connectionString, databaseName, options =>
        {
            // Add development-only condition first
            options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds MongoDB data seeding services with custom assembly patterns.
    /// This is a convenience method for specifying assembly search patterns directly.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="databaseName">MongoDB database name</param>
    /// <param name="assemblyPatterns">Assembly search patterns for auto-discovery</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMongoDbDataSeeding(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string[] assemblyPatterns,
        Action<MongoDbDataSeedingOptions>? configure = null)
    {
        return services.AddMongoDbDataSeeding(connectionString, databaseName, options =>
        {
            // Clear default patterns and add specified ones
            options.AssemblySearchPatterns.Clear();
            foreach (var pattern in assemblyPatterns)
            {
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    options.AssemblySearchPatterns.Add(pattern);
                }
            }
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds MongoDB data seeding services for development with error continuation.
    /// This is a convenience method for development scenarios where seeding errors should not stop the application.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="databaseName">MongoDB database name</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMongoDbDataSeedingWithErrorContinuation(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        Action<MongoDbDataSeedingOptions>? configure = null)
    {
        return services.AddMongoDbDataSeeding(connectionString, databaseName, options =>
        {
            // Configure to continue on errors
            options.IgnoreExceptions = true;
            
            // Add development-only condition for safety
            options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds MongoDB data seeding services with minimal configuration.
    /// Uses connection string and database name, scans all assemblies in the application directory.
    /// Only runs in Development environment for safety.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="databaseName">MongoDB database name</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMongoDbDataSeedingMinimal(
        this IServiceCollection services,
        string connectionString,
        string databaseName)
    {
        return services.AddMongoDbDataSeeding(connectionString, databaseName, options =>
        {
            // Minimal configuration with safety defaults
            options.AssemblySearchPatterns.Clear();
            options.AssemblySearchPatterns.Add("*.dll");
            options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
            options.IgnoreExceptions = true; // Continue on errors for minimal config
        });
    }

    /// <summary>
    /// Adds MongoDB data seeding services with collection dropping enabled.
    /// Use with extreme caution as this will delete all existing data in collections.
    /// Automatically adds development-only condition for safety.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="databaseName">MongoDB database name</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMongoDbDataSeedingWithCollectionDrop(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        Action<MongoDbDataSeedingOptions>? configure = null)
    {
        return services.AddMongoDbDataSeeding(connectionString, databaseName, options =>
        {
            // Enable collection dropping (dangerous!)
            options.DropCollectionsBeforeSeeding = true;
            
            // Force development-only for safety
            options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds MongoDB data seeding services for local development with sensible defaults.
    /// Uses localhost connection, development-only condition, and error continuation.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="databaseName">MongoDB database name</param>
    /// <param name="port">MongoDB port (default: 27017)</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMongoDbDataSeedingForLocalDevelopment(
        this IServiceCollection services,
        string databaseName,
        int port = 27017,
        Action<MongoDbDataSeedingOptions>? configure = null)
    {
        var connectionString = $"mongodb://localhost:{port}";
        
        return services.AddMongoDbDataSeeding(connectionString, databaseName, options =>
        {
            // Development-friendly defaults
            options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
            options.IgnoreExceptions = true;
            options.CreateIndexes = true;
            options.OperationTimeoutSeconds = 60; // Longer timeout for development
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds MongoDB data seeding services with staging environment condition.
    /// This is a convenience method for staging deployments.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="databaseName">MongoDB database name</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMongoDbDataSeedingForStaging(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        Action<MongoDbDataSeedingOptions>? configure = null)
    {
        return services.AddMongoDbDataSeeding(connectionString, databaseName, options =>
        {
            // Add development and staging condition
            options.Conditions.Add(EnvironmentCondition.DevelopmentAndStaging());
            
            // More conservative defaults for staging
            options.IgnoreExceptions = false; // Fail fast in staging
            options.DropCollectionsBeforeSeeding = false; // Never drop in staging
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }
}

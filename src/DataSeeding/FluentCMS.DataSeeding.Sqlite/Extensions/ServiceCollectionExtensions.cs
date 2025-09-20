using System;
using System.Linq;
using FluentCMS.DataSeeding.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.DataSeeding.Sqlite.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register SQLite data seeding services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQLite data seeding services to the dependency injection container.
    /// This is the main entry point for configuring database seeding.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQLite connection string</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddSqliteDataSeeding("Data Source=app.db", options =>
    /// {
    ///     options.AssemblySearchPatterns.Add("MyApp.*.dll");
    ///     options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSqliteDataSeeding(
        this IServiceCollection services,
        string connectionString,
        Action<SqliteDataSeedingOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        // Create and configure options
        var options = new SqliteDataSeedingOptions
        {
            ConnectionString = connectionString
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
            throw new InvalidOperationException($"Invalid SQLite data seeding configuration: {string.Join("; ", validationErrors)}");
        }

        // Register options as singleton
        services.AddSingleton(options);

        // Register the hosted service that will execute seeding
        services.AddHostedService<DataSeedingHostedService>();

        return services;
    }

    /// <summary>
    /// Adds SQLite data seeding services with development environment condition.
    /// This is a convenience method that automatically adds a condition to only run in Development.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQLite connection string</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqliteDataSeedingForDevelopment(
        this IServiceCollection services,
        string connectionString,
        Action<SqliteDataSeedingOptions>? configure = null)
    {
        return services.AddSqliteDataSeeding(connectionString, options =>
        {
            // Add development-only condition first
            options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds SQLite data seeding services with custom assembly patterns.
    /// This is a convenience method for specifying assembly search patterns directly.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQLite connection string</param>
    /// <param name="assemblyPatterns">Assembly search patterns for auto-discovery</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqliteDataSeeding(
        this IServiceCollection services,
        string connectionString,
        string[] assemblyPatterns,
        Action<SqliteDataSeedingOptions>? configure = null)
    {
        return services.AddSqliteDataSeeding(connectionString, options =>
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
    /// Adds SQLite data seeding services for development with error continuation.
    /// This is a convenience method for development scenarios where seeding errors should not stop the application.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQLite connection string</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqliteDataSeedingWithErrorContinuation(
        this IServiceCollection services,
        string connectionString,
        Action<SqliteDataSeedingOptions>? configure = null)
    {
        return services.AddSqliteDataSeeding(connectionString, options =>
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
    /// Adds SQLite data seeding services with minimal configuration.
    /// Uses connection string and scans all assemblies in the application directory.
    /// Only runs in Development environment for safety.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQLite connection string</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqliteDataSeedingMinimal(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddSqliteDataSeeding(connectionString, options =>
        {
            // Minimal configuration with safety defaults
            options.AssemblySearchPatterns.Clear();
            options.AssemblySearchPatterns.Add("*.dll");
            options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
            options.IgnoreExceptions = true; // Continue on errors for minimal config
        });
    }
}

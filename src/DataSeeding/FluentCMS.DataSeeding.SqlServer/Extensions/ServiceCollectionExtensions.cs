using System;
using System.Linq;
using FluentCMS.DataSeeding.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.DataSeeding.SqlServer.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register SQL Server data seeding services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQL Server data seeding services to the dependency injection container.
    /// This is the main entry point for configuring database seeding.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddSqlServerDataSeeding("Server=localhost;Database=MyApp;Trusted_Connection=true", options =>
    /// {
    ///     options.AssemblySearchPatterns.Add("MyApp.*.dll");
    ///     options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
    ///     options.DefaultSchema = "app";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSqlServerDataSeeding(
        this IServiceCollection services,
        string connectionString,
        Action<SqlServerDataSeedingOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        // Create and configure options
        var options = new SqlServerDataSeedingOptions
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
            throw new InvalidOperationException($"Invalid SQL Server data seeding configuration: {string.Join("; ", validationErrors)}");
        }

        // Register options as singleton
        services.AddSingleton(options);

        // Register the hosted service that will execute seeding
        services.AddHostedService<DataSeedingHostedService>();

        return services;
    }

    /// <summary>
    /// Adds SQL Server data seeding services with development environment condition.
    /// This is a convenience method that automatically adds a condition to only run in Development.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqlServerDataSeedingForDevelopment(
        this IServiceCollection services,
        string connectionString,
        Action<SqlServerDataSeedingOptions>? configure = null)
    {
        return services.AddSqlServerDataSeeding(connectionString, options =>
        {
            // Add development-only condition first
            options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds SQL Server data seeding services with custom assembly patterns.
    /// This is a convenience method for specifying assembly search patterns directly.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="assemblyPatterns">Assembly search patterns for auto-discovery</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqlServerDataSeeding(
        this IServiceCollection services,
        string connectionString,
        string[] assemblyPatterns,
        Action<SqlServerDataSeedingOptions>? configure = null)
    {
        return services.AddSqlServerDataSeeding(connectionString, options =>
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
    /// Adds SQL Server data seeding services for development with error continuation.
    /// This is a convenience method for development scenarios where seeding errors should not stop the application.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqlServerDataSeedingWithErrorContinuation(
        this IServiceCollection services,
        string connectionString,
        Action<SqlServerDataSeedingOptions>? configure = null)
    {
        return services.AddSqlServerDataSeeding(connectionString, options =>
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
    /// Adds SQL Server data seeding services with minimal configuration.
    /// Uses connection string and scans all assemblies in the application directory.
    /// Only runs in Development environment for safety.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqlServerDataSeedingMinimal(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddSqlServerDataSeeding(connectionString, options =>
        {
            // Minimal configuration with safety defaults
            options.AssemblySearchPatterns.Clear();
            options.AssemblySearchPatterns.Add("*.dll");
            options.Conditions.Add(EnvironmentCondition.DevelopmentOnly());
            options.IgnoreExceptions = true; // Continue on errors for minimal config
        });
    }

    /// <summary>
    /// Adds SQL Server data seeding services with production-safe configuration.
    /// Includes strict error handling and conditional execution based on configuration values.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="configurationKey">Configuration key to check for enabling seeding (e.g., "DataSeeding:Enabled")</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqlServerDataSeedingForProduction(
        this IServiceCollection services,
        string connectionString,
        string configurationKey,
        Action<SqlServerDataSeedingOptions>? configure = null)
    {
        return services.AddSqlServerDataSeeding(connectionString, options =>
        {
            // Add configuration-based condition for production safety
            options.Conditions.Add(ConfigurationCondition.IsTrue(configurationKey));
            
            // Production-safe defaults
            options.IgnoreExceptions = false; // Fail fast in production
            options.UseTransactions = true; // Ensure transactional integrity
            options.CreateDatabaseIfNotExists = false; // Don't auto-create in production
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds SQL Server data seeding services with custom schema configuration.
    /// Useful for multi-tenant applications or applications with custom schema requirements.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="defaultSchema">Default schema name for seeding operations</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqlServerDataSeedingWithSchema(
        this IServiceCollection services,
        string connectionString,
        string defaultSchema,
        Action<SqlServerDataSeedingOptions>? configure = null)
    {
        return services.AddSqlServerDataSeeding(connectionString, options =>
        {
            // Set custom schema
            options.DefaultSchema = defaultSchema;
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds SQL Server data seeding services with transaction configuration.
    /// Allows fine-grained control over transaction behavior.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="useTransactions">Whether to use transactions for seeding operations</param>
    /// <param name="isolationLevel">Transaction isolation level</param>
    /// <param name="configure">Optional configuration action for seeding options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSqlServerDataSeedingWithTransactions(
        this IServiceCollection services,
        string connectionString,
        bool useTransactions,
        System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted,
        Action<SqlServerDataSeedingOptions>? configure = null)
    {
        return services.AddSqlServerDataSeeding(connectionString, options =>
        {
            // Configure transaction behavior
            options.UseTransactions = useTransactions;
            options.IsolationLevel = isolationLevel;
            
            // Apply user configuration after
            configure?.Invoke(options);
        });
    }
}

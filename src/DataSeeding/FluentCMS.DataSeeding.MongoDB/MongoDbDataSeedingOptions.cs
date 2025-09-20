using System.Collections.Generic;
using FluentCMS.DataSeeding.Abstractions;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeding.MongoDB;

/// <summary>
/// Configuration options for MongoDB data seeding operations.
/// </summary>
public class MongoDbDataSeedingOptions
{
    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MongoDB database name.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the collection of assembly search patterns for auto-discovery.
    /// Use wildcard patterns like "MyApp.*.dll" to discover seeders and validators.
    /// </summary>
    public List<string> AssemblySearchPatterns { get; } = new();

    /// <summary>
    /// Gets the collection of conditions that must all pass for seeding to execute.
    /// If any condition fails, seeding is skipped entirely.
    /// </summary>
    public List<ICondition> Conditions { get; } = new();

    /// <summary>
    /// Gets or sets whether to continue execution when individual seeders or validators fail.
    /// When true, errors are logged but don't stop the overall seeding process.
    /// When false (default), the first error stops all seeding operations.
    /// </summary>
    public bool IgnoreExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum log level for seeding operations.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets whether to create the MongoDB database if it doesn't exist.
    /// MongoDB creates databases automatically when collections are created.
    /// </summary>
    public bool CreateDatabaseIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets the operation timeout in seconds for MongoDB operations.
    /// Default is 30 seconds.
    /// </summary>
    public int OperationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable detailed tracing of MongoDB operations.
    /// Useful for debugging but may impact performance.
    /// </summary>
    public bool EnableMongoTracing { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to use SSL/TLS for MongoDB connections.
    /// Default is false for local development.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of connections in the connection pool.
    /// Default is 100.
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the minimum number of connections in the connection pool.
    /// Default is 0.
    /// </summary>
    public int MinConnectionPoolSize { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to drop existing collections before seeding.
    /// Use with caution as this will delete all existing data.
    /// </summary>
    public bool DropCollectionsBeforeSeeding { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to create indexes during schema validation.
    /// Default is true.
    /// </summary>
    public bool CreateIndexes { get; set; } = true;

    /// <summary>
    /// Validates the configuration and returns any validation errors.
    /// </summary>
    /// <returns>Collection of validation error messages</returns>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            errors.Add("ConnectionString is required");
        }

        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            errors.Add("DatabaseName is required");
        }

        if (AssemblySearchPatterns.Count == 0)
        {
            errors.Add("At least one AssemblySearchPattern is required");
        }

        foreach (var pattern in AssemblySearchPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                errors.Add("AssemblySearchPattern cannot be null or empty");
            }
        }

        if (OperationTimeoutSeconds <= 0)
        {
            errors.Add("OperationTimeoutSeconds must be greater than 0");
        }

        if (MaxConnectionPoolSize <= 0)
        {
            errors.Add("MaxConnectionPoolSize must be greater than 0");
        }

        if (MinConnectionPoolSize < 0)
        {
            errors.Add("MinConnectionPoolSize cannot be negative");
        }

        if (MinConnectionPoolSize > MaxConnectionPoolSize)
        {
            errors.Add("MinConnectionPoolSize cannot be greater than MaxConnectionPoolSize");
        }

        return errors;
    }

    /// <summary>
    /// Creates a copy of this options instance with default assembly search patterns.
    /// </summary>
    /// <param name="defaultPatterns">Default patterns to add if no patterns are configured</param>
    /// <returns>A new options instance with default patterns applied</returns>
    public MongoDbDataSeedingOptions WithDefaultPatterns(params string[] defaultPatterns)
    {
        var copy = new MongoDbDataSeedingOptions
        {
            ConnectionString = ConnectionString,
            DatabaseName = DatabaseName,
            IgnoreExceptions = IgnoreExceptions,
            LogLevel = LogLevel,
            CreateDatabaseIfNotExists = CreateDatabaseIfNotExists,
            OperationTimeoutSeconds = OperationTimeoutSeconds,
            EnableMongoTracing = EnableMongoTracing,
            UseSsl = UseSsl,
            MaxConnectionPoolSize = MaxConnectionPoolSize,
            MinConnectionPoolSize = MinConnectionPoolSize,
            DropCollectionsBeforeSeeding = DropCollectionsBeforeSeeding,
            CreateIndexes = CreateIndexes
        };

        // Copy existing patterns
        foreach (var pattern in AssemblySearchPatterns)
        {
            copy.AssemblySearchPatterns.Add(pattern);
        }

        // Add default patterns if none exist
        if (copy.AssemblySearchPatterns.Count == 0 && defaultPatterns.Length > 0)
        {
            foreach (var pattern in defaultPatterns)
            {
                copy.AssemblySearchPatterns.Add(pattern);
            }
        }

        // Copy conditions
        foreach (var condition in Conditions)
        {
            copy.Conditions.Add(condition);
        }

        return copy;
    }

    /// <summary>
    /// Gets a summary of the current configuration for logging purposes.
    /// </summary>
    /// <returns>Dictionary of configuration values</returns>
    public Dictionary<string, object> GetConfigurationSummary()
    {
        return new Dictionary<string, object>
        {
            ["HasConnectionString"] = !string.IsNullOrWhiteSpace(ConnectionString),
            ["DatabaseName"] = DatabaseName,
            ["AssemblyPatternCount"] = AssemblySearchPatterns.Count,
            ["AssemblyPatterns"] = AssemblySearchPatterns.ToArray(),
            ["ConditionCount"] = Conditions.Count,
            ["IgnoreExceptions"] = IgnoreExceptions,
            ["LogLevel"] = LogLevel.ToString(),
            ["CreateDatabaseIfNotExists"] = CreateDatabaseIfNotExists,
            ["OperationTimeoutSeconds"] = OperationTimeoutSeconds,
            ["EnableMongoTracing"] = EnableMongoTracing,
            ["UseSsl"] = UseSsl,
            ["MaxConnectionPoolSize"] = MaxConnectionPoolSize,
            ["MinConnectionPoolSize"] = MinConnectionPoolSize,
            ["DropCollectionsBeforeSeeding"] = DropCollectionsBeforeSeeding,
            ["CreateIndexes"] = CreateIndexes
        };
    }
}

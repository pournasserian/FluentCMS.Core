using System.Collections.Generic;
using FluentCMS.DataSeeding.Abstractions;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeding.SqlServer;

/// <summary>
/// Configuration options for SQL Server data seeding operations.
/// </summary>
public class SqlServerDataSeedingOptions
{
    /// <summary>
    /// Gets or sets the SQL Server connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

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
    /// Gets or sets whether to create the SQL Server database if it doesn't exist.
    /// Requires appropriate permissions and database server access.
    /// </summary>
    public bool CreateDatabaseIfNotExists { get; set; } = false;

    /// <summary>
    /// Gets or sets the command timeout in seconds for database operations.
    /// Default is 30 seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable detailed tracing of SQL operations.
    /// Useful for debugging but may impact performance.
    /// </summary>
    public bool EnableSqlTracing { get; set; } = false;

    /// <summary>
    /// Gets or sets the default database schema name for seeding operations.
    /// Defaults to "dbo".
    /// </summary>
    public string DefaultSchema { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets whether to use SQL Server transactions for each seeder.
    /// When true, each seeder runs in its own transaction.
    /// </summary>
    public bool UseTransactions { get; set; } = true;

    /// <summary>
    /// Gets or sets the SQL Server isolation level for seeding transactions.
    /// </summary>
    public System.Data.IsolationLevel IsolationLevel { get; set; } = System.Data.IsolationLevel.ReadCommitted;

    /// <summary>
    /// Gets or sets whether to enable Multiple Active Result Sets (MARS) in the connection.
    /// </summary>
    public bool EnableMars { get; set; } = false;

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

        if (CommandTimeoutSeconds <= 0)
        {
            errors.Add("CommandTimeoutSeconds must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(DefaultSchema))
        {
            errors.Add("DefaultSchema cannot be null or empty");
        }

        return errors;
    }

    /// <summary>
    /// Creates a copy of this options instance with default assembly search patterns.
    /// </summary>
    /// <param name="defaultPatterns">Default patterns to add if no patterns are configured</param>
    /// <returns>A new options instance with default patterns applied</returns>
    public SqlServerDataSeedingOptions WithDefaultPatterns(params string[] defaultPatterns)
    {
        var copy = new SqlServerDataSeedingOptions
        {
            ConnectionString = ConnectionString,
            IgnoreExceptions = IgnoreExceptions,
            LogLevel = LogLevel,
            CreateDatabaseIfNotExists = CreateDatabaseIfNotExists,
            CommandTimeoutSeconds = CommandTimeoutSeconds,
            EnableSqlTracing = EnableSqlTracing,
            DefaultSchema = DefaultSchema,
            UseTransactions = UseTransactions,
            IsolationLevel = IsolationLevel,
            EnableMars = EnableMars
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
            ["AssemblyPatternCount"] = AssemblySearchPatterns.Count,
            ["AssemblyPatterns"] = AssemblySearchPatterns.ToArray(),
            ["ConditionCount"] = Conditions.Count,
            ["IgnoreExceptions"] = IgnoreExceptions,
            ["LogLevel"] = LogLevel.ToString(),
            ["CreateDatabaseIfNotExists"] = CreateDatabaseIfNotExists,
            ["CommandTimeoutSeconds"] = CommandTimeoutSeconds,
            ["EnableSqlTracing"] = EnableSqlTracing,
            ["DefaultSchema"] = DefaultSchema,
            ["UseTransactions"] = UseTransactions,
            ["IsolationLevel"] = IsolationLevel.ToString(),
            ["EnableMars"] = EnableMars
        };
    }
}

namespace FluentCMS.DataSeeder;

/// <summary>
/// Configuration options for the database seeding process
/// </summary>
public class SeedingOptions
{
    /// <summary>
    /// List of conditions that must be met before seeding occurs
    /// </summary>
    public List<ISeedingCondition> Conditions { get; set; } = [];

    /// <summary>
    /// Whether to create the database schema if it doesn't exist
    /// </summary>
    public bool EnsureSchemaCreated { get; set; } = true;

    /// <summary>
    /// Whether to run seeders in a transaction
    /// </summary>
    public bool UseTransaction { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts for seeding operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to log seeding operations
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}

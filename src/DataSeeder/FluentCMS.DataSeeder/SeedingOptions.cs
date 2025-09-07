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
    /// Whether to log seeding operations
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// List of assembly name prefixes to scan for seeders and conditions
    /// </summary>
    public List<string> AssemblyPrefixesToScan { get; set; } = [];

    /// <summary>
    /// Whether to ignore exceptions during the seeding process
    /// </summary>
    public bool IgnoreExceptions { get; set; } = false;
}

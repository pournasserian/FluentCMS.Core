namespace FluentCMS.DataSeeder.Abstractions;

/// <summary>
/// Interface for defining conditions that must be met before seeding occurs
/// </summary>
public interface ISeedingCondition
{
    /// <summary>
    /// Checks if the condition is met for seeding to proceed
    /// </summary>
    /// <returns>True if seeding should proceed, false otherwise</returns>
    Task<bool> ShouldSeed();

    /// <summary>
    /// Name of the condition for logging purposes
    /// </summary>
    string Name { get; }
}

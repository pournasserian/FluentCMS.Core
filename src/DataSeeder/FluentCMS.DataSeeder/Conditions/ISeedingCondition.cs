using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataSeeder.Conditions;

/// <summary>
/// Interface for defining conditions that must be met before seeding occurs
/// </summary>
public interface ISeedingCondition
{
    /// <summary>
    /// Checks if the condition is met for seeding to proceed
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>True if seeding should proceed, false otherwise</returns>
    Task<bool> ShouldSeed(DbContext context);

    /// <summary>
    /// Name of the condition for logging purposes
    /// </summary>
    string Name { get; }
}

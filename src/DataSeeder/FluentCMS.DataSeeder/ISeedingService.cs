using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataSeeder;

/// <summary>
/// Main service interface for orchestrating the database seeding process
/// </summary>
public interface ISeedingService
{
    /// <summary>
    /// Executes the complete seeding process including schema creation and data seeding
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ExecuteSeeding(DbContext context);

    /// <summary>
    /// Checks if seeding conditions are met
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>True if all conditions are met, false otherwise</returns>
    Task<bool> CanSeed(DbContext context);

    /// <summary>
    /// Creates the database schema if it doesn't exist
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task EnsureSchema(DbContext context);

    /// <summary>
    /// Seeds the initial data
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SeedData(DbContext context);
}

namespace FluentCMS.DataSeeder.Abstractions;

/// <summary>
/// Main service interface for orchestrating the database seeding process
/// </summary>
public interface ISeedingService
{
    /// <summary>
    /// Executes the complete seeding process including schema creation and data seeding
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ExecuteSeeding();

    /// <summary>
    /// Checks if seeding conditions are met
    /// </summary>
    /// <returns>True if all conditions are met, false otherwise</returns>
    Task<bool> CanSeed();

    /// <summary>
    /// Creates the database schema if it doesn't exist
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task EnsureSchema();

    /// <summary>
    /// Seeds the initial data
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SeedData();
}

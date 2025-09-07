namespace FluentCMS.DataSeeder.Abstractions;

/// <summary>
/// Interface for defining seed data
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Seeds data into the target data store
    /// </summary>
    Task SeedData(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if seeding conditions are met
    /// </summary>
    Task<bool> ShouldSeed(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the database schema needs to be created
    /// </summary>
    Task<bool> ShouldCreateSchema(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the database schema
    /// </summary>
    Task CreateSchema(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execution order for this seeder (lower values execute first)
    /// </summary>
    int Order { get; }
}

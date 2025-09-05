namespace FluentCMS.DataSeeder.Abstractions;

/// <summary>
/// Interface for defining seed data
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Seeds data into the target data store
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task Seed();

    /// <summary>
    /// Execution order for this seeder (lower values execute first)
    /// </summary>
    int Order { get; }
}

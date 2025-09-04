using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataSeeder;

/// <summary>
/// Interface for defining seed data for a specific entity type
/// </summary>
/// <typeparam name="T">The entity type to seed</typeparam>
public interface ISeeder<T> where T : class
{
    /// <summary>
    /// Seeds data for the specified entity type
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task Seed(DbContext context);

    /// <summary>
    /// Execution order for this seeder (lower values execute first)
    /// </summary>
    int Order { get; }
}

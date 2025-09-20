using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Models;

namespace FluentCMS.DataSeeding.Abstractions;

/// <summary>
/// Defines a contract for validating and creating database schemas.
/// Schema validators run before data seeders to ensure required database structures exist.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Execution priority for this validator. Lower numbers execute first.
    /// Schema validators typically use priorities 1-99, while data seeders use 100+.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Validates whether the required database schema exists and is properly configured.
    /// This should check for tables, indexes, constraints, and other schema elements.
    /// </summary>
    /// <param name="context">The seeding context providing database access and services</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if schema is valid, false if CreateSchema should be called</returns>
    Task<bool> ValidateSchema(SeedingContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the required database schema elements. This method should only be called
    /// if ValidateSchema returns false, ensuring schemas are created only when needed.
    /// </summary>
    /// <param name="context">The seeding context providing database access and services</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task CreateSchema(SeedingContext context, CancellationToken cancellationToken = default);
}

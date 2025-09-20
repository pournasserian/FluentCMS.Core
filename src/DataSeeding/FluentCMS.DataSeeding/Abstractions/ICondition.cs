using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Models;

namespace FluentCMS.DataSeeding.Abstractions;

/// <summary>
/// Defines a contract for conditional execution of seeding operations.
/// Conditions determine whether seeding should proceed based on environment,
/// configuration, or other runtime factors.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// Evaluates whether seeding operations should execute based on this condition.
    /// Multiple conditions are evaluated with AND logic by default.
    /// </summary>
    /// <param name="context">The seeding context providing database access and services</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if seeding should proceed, false to skip seeding</returns>
    Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default);
}

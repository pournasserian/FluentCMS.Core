using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Abstractions;
using FluentCMS.DataSeeding.Models;

namespace FluentCMS.DataSeeding.Conditions;

/// <summary>
/// A condition that combines multiple conditions using logical operators.
/// Enables complex conditional logic for seeding operations.
/// </summary>
public class CompositeCondition : ICondition
{
    private readonly ICondition[] _conditions;
    private readonly LogicalOperator _operator;

    /// <summary>
    /// Initializes a new instance of CompositeCondition with conditions and logical operator.
    /// </summary>
    /// <param name="logicalOperator">The logical operator to use for combining conditions</param>
    /// <param name="conditions">The conditions to combine</param>
    public CompositeCondition(LogicalOperator logicalOperator, params ICondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            throw new ArgumentException("At least one condition must be provided", nameof(conditions));

        _operator = logicalOperator;
        _conditions = conditions;
    }

    /// <summary>
    /// Creates a composite condition that requires ALL conditions to pass (AND logic).
    /// </summary>
    /// <param name="conditions">The conditions that must all pass</param>
    /// <returns>A composite condition using AND logic</returns>
    public static CompositeCondition All(params ICondition[] conditions)
    {
        return new CompositeCondition(LogicalOperator.And, conditions);
    }

    /// <summary>
    /// Creates a composite condition that requires ANY condition to pass (OR logic).
    /// </summary>
    /// <param name="conditions">The conditions where at least one must pass</param>
    /// <returns>A composite condition using OR logic</returns>
    public static CompositeCondition Any(params ICondition[] conditions)
    {
        return new CompositeCondition(LogicalOperator.Or, conditions);
    }

    /// <summary>
    /// Creates a composite condition that requires NONE of the conditions to pass (NOR logic).
    /// </summary>
    /// <param name="conditions">The conditions that must all fail</param>
    /// <returns>A composite condition using NOR logic</returns>
    public static CompositeCondition None(params ICondition[] conditions)
    {
        return new CompositeCondition(LogicalOperator.Nor, conditions);
    }

    /// <summary>
    /// Evaluates the composite condition based on the configured logical operator.
    /// </summary>
    /// <param name="context">The seeding context providing database access and services</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if the composite condition passes, false otherwise</returns>
    public async Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return _operator switch
            {
                LogicalOperator.And => await EvaluateAll(context, cancellationToken),
                LogicalOperator.Or => await EvaluateAny(context, cancellationToken),
                LogicalOperator.Nor => !await EvaluateAny(context, cancellationToken),
                _ => false
            };
        }
        catch (Exception)
        {
            // If evaluation fails, default to not executing for safety
            return false;
        }
    }

    /// <summary>
    /// Evaluates all conditions and returns true only if ALL pass (AND logic).
    /// </summary>
    private async Task<bool> EvaluateAll(SeedingContext context, CancellationToken cancellationToken)
    {
        foreach (var condition in _conditions)
        {
            var result = await condition.ShouldExecute(context, cancellationToken);
            if (!result)
            {
                // Short-circuit: if any condition fails, the whole AND expression fails
                return false;
            }
        }

        // All conditions passed
        return true;
    }

    /// <summary>
    /// Evaluates all conditions and returns true if ANY pass (OR logic).
    /// </summary>
    private async Task<bool> EvaluateAny(SeedingContext context, CancellationToken cancellationToken)
    {
        foreach (var condition in _conditions)
        {
            var result = await condition.ShouldExecute(context, cancellationToken);
            if (result)
            {
                // Short-circuit: if any condition passes, the whole OR expression passes
                return true;
            }
        }

        // No conditions passed
        return false;
    }

    /// <summary>
    /// Gets the conditions included in this composite condition.
    /// </summary>
    public IReadOnlyCollection<ICondition> Conditions => _conditions.AsReadOnly();

    /// <summary>
    /// Gets the logical operator used by this composite condition.
    /// </summary>
    public LogicalOperator Operator => _operator;
}

/// <summary>
/// Defines logical operators for combining conditions.
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// Logical AND - all conditions must pass.
    /// </summary>
    And,

    /// <summary>
    /// Logical OR - at least one condition must pass.
    /// </summary>
    Or,

    /// <summary>
    /// Logical NOR - no conditions may pass (all must fail).
    /// </summary>
    Nor
}

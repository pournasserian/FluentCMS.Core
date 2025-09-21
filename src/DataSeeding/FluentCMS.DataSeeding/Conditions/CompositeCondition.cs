using FluentCMS.DataSeeding.Abstractions;

namespace FluentCMS.DataSeeding.Conditions;

/// <summary>
/// Condition that combines multiple conditions with AND/OR logic.
/// This allows for complex conditional logic by composing simpler conditions.
/// </summary>
/// <param name="useAndLogic">
/// True for AND logic (all conditions must be true), 
/// False for OR logic (at least one condition must be true)
/// </param>
/// <param name="conditions">Array of conditions to combine</param>
public class CompositeCondition(bool useAndLogic, params ICondition[] conditions) : ICondition
{
    // Store conditions in a list for easier manipulation and enumeration
    private readonly List<ICondition> _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));

    /// <summary>
    /// Gets a descriptive name that shows the logic type and included conditions
    /// </summary>
    public string Name => $"Composite ({(useAndLogic ? "AND" : "OR")}): {string.Join(", ", _conditions.Select(c => c.Name))}";

    /// <summary>
    /// Evaluates all contained conditions using the specified logic (AND/OR)
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the evaluation process</param>
    /// <returns>
    /// For AND logic: true if all conditions return true, false if any condition returns false
    /// For OR logic: true if any condition returns true, false if all conditions return false
    /// Empty condition list always returns true
    /// </returns>
    public async Task<bool> ShouldExecute(CancellationToken cancellationToken = default)
    {
        // Empty conditions should not block execution
        if (_conditions.Count == 0)
            return true;

        try
        {
            if (useAndLogic)
            {
                // AND logic - all conditions must be true
                // Short-circuit evaluation: return false as soon as any condition fails
                foreach (var condition in _conditions)
                {
                    if (!await condition.ShouldExecute(cancellationToken))
                        return false; // One false condition makes the entire AND expression false
                }
                return true; // All conditions passed
            }
            else
            {
                // OR logic - at least one condition must be true
                // Short-circuit evaluation: return true as soon as any condition succeeds
                foreach (var condition in _conditions)
                {
                    if (await condition.ShouldExecute(cancellationToken))
                        return true; // One true condition makes the entire OR expression true
                }
                return false; // No conditions passed
            }
        }
        catch
        {
            // If any condition throws an exception, treat it as a failure
            // This ensures seeding doesn't proceed with potentially invalid conditions
            return false;
        }
    }
}
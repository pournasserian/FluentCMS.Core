using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataSeeder.Conditions;

/// <summary>
/// Condition that combines multiple conditions with AND/OR logic
/// </summary>
public class CompositeCondition(bool useAndLogic, params ISeedingCondition[] conditions) : ISeedingCondition
{
    private readonly List<ISeedingCondition> _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));

    public string Name => $"Composite ({(useAndLogic ? "AND" : "OR")}): {string.Join(", ", _conditions.Select(c => c.Name))}";

    public async Task<bool> ShouldSeed(DbContext context)
    {
        if (_conditions.Count == 0)
            return true;

        try
        {
            if (useAndLogic)
            {
                // AND logic - all conditions must be true
                foreach (var condition in _conditions)
                {
                    if (!await condition.ShouldSeed(context))
                        return false;
                }
                return true;
            }
            else
            {
                // OR logic - at least one condition must be true
                foreach (var condition in _conditions)
                {
                    if (await condition.ShouldSeed(context))
                        return true;
                }
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
}
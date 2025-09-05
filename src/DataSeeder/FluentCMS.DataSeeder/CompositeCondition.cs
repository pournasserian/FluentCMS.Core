namespace FluentCMS.DataSeeder;

/// <summary>
/// Condition that combines multiple conditions with AND/OR logic
/// </summary>
public class CompositeCondition(bool useAndLogic, params ISeedingCondition[] conditions) : ISeedingCondition
{
    private readonly List<ISeedingCondition> _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));

    public string Name => $"Composite ({(useAndLogic ? "AND" : "OR")}): {string.Join(", ", _conditions.Select(c => c.Name))}";

    public async Task<bool> ShouldSeed()
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
                    if (!await condition.ShouldSeed())
                        return false;
                }
                return true;
            }
            else
            {
                // OR logic - at least one condition must be true
                foreach (var condition in _conditions)
                {
                    if (await condition.ShouldSeed())
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
namespace FluentCMS.DataSeeder;

/// <summary>
/// Condition that checks the current hosting environment
/// </summary>
public class EnvironmentCondition(IHostEnvironment environment, Func<IHostEnvironment, bool> predicate) : ISeedingCondition
{
    public string Name => "Environment Condition";

    public Task<bool> ShouldSeed()
    {
        try
        {
            return Task.FromResult(predicate(environment));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

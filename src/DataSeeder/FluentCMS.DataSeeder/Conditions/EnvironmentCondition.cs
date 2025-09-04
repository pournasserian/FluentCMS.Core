using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.DataSeeder.Conditions;

/// <summary>
/// Condition that checks the current hosting environment
/// </summary>
public class EnvironmentCondition(IHostEnvironment environment, Func<IHostEnvironment, bool> predicate) : ISeedingCondition
{
    public string Name => "Environment Condition";

    public Task<bool> ShouldSeed(DbContext context)
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

using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataSeeder.Conditions;

/// <summary>
/// Condition that checks if the database exists
/// </summary>
public class DatabaseExistsCondition : ISeedingCondition
{
    public string Name => "Database Exists";

    public async Task<bool> ShouldSeed(DbContext context)
    {
        try
        {
            return await context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
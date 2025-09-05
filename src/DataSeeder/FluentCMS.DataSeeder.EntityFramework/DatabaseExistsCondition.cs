namespace FluentCMS.DataSeeder.EntityFramework;

/// <summary>
/// Condition that checks if the database exists
/// </summary>
public class DatabaseExistsCondition<TContext>(TContext context) : ISeedingCondition where TContext : DbContext
{
    public string Name => "Database Exists";

    public async Task<bool> ShouldSeed()
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
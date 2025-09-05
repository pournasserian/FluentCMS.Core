namespace FluentCMS.DataSeeder.EntityFramework;

/// <summary>
/// Condition that checks if specified tables are empty
/// </summary>
public class TablesEmptyCondition<TContext>(TContext context, params string[] tableNames) : ISeedingCondition where TContext : DbContext
{
    public string Name => $"Tables Empty: {string.Join(", ", tableNames)}";

    public async Task<bool> ShouldSeed()
    {
        try
        {
            foreach (var tableName in tableNames)
            {
                var entityType = context.Model.GetEntityTypes()
                    .FirstOrDefault(e => e.GetTableName() == tableName);

                if (entityType != null)
                {
                    // Use raw SQL to count rows in the table
                    var actualTableName = entityType.GetTableName();
                    var sql = $"SELECT COUNT(*) FROM [{actualTableName}]";

                    var connection = context.Database.GetDbConnection();
                    await context.Database.OpenConnectionAsync();

                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    var result = await command.ExecuteScalarAsync();
                    var count = Convert.ToInt32(result);

                    if (count > 0)
                    {
                        return false; // Table is not empty
                    }
                }
            }
            return true; // All specified tables are empty
        }
        catch
        {
            return false;
        }
    }
}

namespace FluentCMS.Plugins.AuditTrailManager.Repositories;

public class AuditTrailSeeder(AuditTrailDbContext dbContext, IDatabaseManager databaseManager) : ISeeder
{
    public int Order => 10;

    public async Task CreateSchema(CancellationToken cancellationToken = default)
    {
        await databaseManager.CreateDatabase(cancellationToken);
        var sql = dbContext.Database.GenerateCreateScript();
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public Task SeedData(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> ShouldCreateSchema(CancellationToken cancellationToken = default)
    {
        if (!await databaseManager.DatabaseExists(cancellationToken))
            return true;
        return !await databaseManager.TablesExist(["AuditTrails"], cancellationToken);
    }

    public Task<bool> ShouldSeed(CancellationToken cancellationToken = default)
    {
        return databaseManager.TablesEmpty(["AuditTrails"], cancellationToken);
    }
}

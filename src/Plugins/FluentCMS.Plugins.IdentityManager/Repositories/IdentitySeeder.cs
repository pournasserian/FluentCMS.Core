namespace FluentCMS.Plugins.IdentityManager.Repositories;

public class AuditTrailSeeder(ApplicationDbContext dbContext, IDatabaseManager databaseManager) : ISeeder
{
    public int Order => 100;

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
        return !await databaseManager.TablesExist(["Users", "Roles"], cancellationToken);
    }

    public Task<bool> ShouldSeed(CancellationToken cancellationToken = default)
    {
        return databaseManager.TablesEmpty(["Users", "Roles"], cancellationToken);
    }
}

namespace FluentCMS.Plugins.IdentityManager;

public class IdentitySeeder(ApplicationDbContext dbContext, IDatabaseManager databaseManager) : ISeeder
{
    public int Order => 1000;

    public async Task CreateSchema(CancellationToken cancellationToken = default)
    {
        await databaseManager.CreateDatabase(cancellationToken);
        var sql = dbContext.Database.GenerateCreateScript();
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public async Task SeedData(CancellationToken cancellationToken = default)
    {
        var roles = new[]
        {
            new Role { Name = "Admin", NormalizedName = "ADMIN" },
            new Role { Name = "User", NormalizedName = "USER" }
        };
        await dbContext.Roles.AddRangeAsync(roles);
        await dbContext.SaveChangesAsync(cancellationToken);

    }

    public async Task<bool> ShouldCreateSchema(CancellationToken cancellationToken = default)
    {
        if (!await databaseManager.DatabaseExists(cancellationToken))
            return true;
        return !await databaseManager.TablesExist(["Users", "Roles"], cancellationToken);
    }

    public Task<bool> ShouldSeed(CancellationToken cancellationToken = default)
    {
        return databaseManager.TablesEmpty(["Roles"], cancellationToken);
    }
}

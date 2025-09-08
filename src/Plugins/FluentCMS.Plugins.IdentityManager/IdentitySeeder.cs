using FluentCMS.Configuration.EntityFramework;

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


public class IdentityOptionsSeeder(IConfiguration configuration, ISettingService settingService) : ISeeder
{
    public int Order => 100;

    public Task CreateSchema(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task SeedData(CancellationToken cancellationToken = default)
    {
        var passwordOptions = new PasswordOptions();
        configuration.GetSection("IdentityOptions:Password").Bind(passwordOptions);
        await settingService.Update("IdentityOptions:Password", passwordOptions, cancellationToken);
    }

    public Task<bool> ShouldCreateSchema(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public async Task<bool> ShouldSeed(CancellationToken cancellationToken = default)
    {
        var allSettings = await settingService.GetAllKeys(cancellationToken);
        return !allSettings.Contains("IdentityOptions:Password");
    }
}

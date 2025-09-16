using FluentCMS.DataSeeder.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FluentCMS.Providers.Repositories.EntityFramework;

internal class ProviderSeeder(ProviderDbContext dbContext, IDatabaseManager databaseManager, IConfiguration configuration, IProviderManager providerManager) : ISeeder
{
    public int Order => 1;

    public async Task<bool> ShouldCreateSchema(CancellationToken cancellationToken = default)
    {
        if (!await databaseManager.DatabaseExists(cancellationToken))
            return true;
        return !await databaseManager.TablesExist(["Providers"], cancellationToken);
    }

    public async Task CreateSchema(CancellationToken cancellationToken = default)
    {
        await databaseManager.CreateDatabase(cancellationToken);
        var sql = dbContext.Database.GenerateCreateScript();
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public async Task<bool> ShouldSeed(CancellationToken cancellationToken = default)
    {
        return await databaseManager.TablesEmpty(["Providers"], cancellationToken);
    }

    public async Task SeedData(CancellationToken cancellationToken = default)
    {
        var providerRoot = configuration.GetSection("Providers").Get<ProviderConfigurationRoot>() ?? [];
        foreach (var (area, providerAreas) in providerRoot)
        {
            foreach (var providerArea in providerAreas)
            {
                var providerModule = await providerManager.GetProviderModule(area, providerArea.Module, cancellationToken) ??
                    throw new InvalidOperationException($"Provider module '{providerArea.Module}' for area '{area}' not found.");

                var providerCatalog = new ProviderCatalog(providerModule, providerArea.Name, providerArea.Active ?? false);

                await providerManager.Add(providerCatalog, cancellationToken);
            }
        }
    }

    internal class ProviderConfigurationRoot : Dictionary<string, List<ProviderAreaConfiguration>>
    {
    }

    internal class ProviderAreaConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public bool? Active { get; set; }
        public string Module { get; set; } = string.Empty;
        public Dictionary<string, object?>? Options { get; set; } // stays as dictionary
    }
}

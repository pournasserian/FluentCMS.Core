using FluentCMS.DataSeeder.Abstractions;
using FluentCMS.Providers.Repositories.Abstractions;
using FluentCMS.Providers.Repositories.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers.Repositories.EntityFramework;

public class ProviderSeeder(IServiceProvider sp) : ISeeder
{
    private readonly ProviderDbContext? _dbContext = sp.GetService<ProviderDbContext>();
    private readonly IDatabaseManager? _databaseManager = sp.GetService<IDatabaseManager>();
    private readonly IProviderManager? _providerManager = sp.GetService<IProviderManager>();
    private readonly IProviderRepository? _providerRepository = sp.GetService<IProviderRepository>();
    private readonly ConfigurationReadOnlyProviderRepository? _readOnlyProviderRepository = sp.GetService<ConfigurationReadOnlyProviderRepository>();
    private readonly bool _isActive = sp.GetService<ProviderDbContext>() is not null;

    public int Order => 1;

    public async Task<bool> ShouldCreateSchema(CancellationToken cancellationToken = default)
    {
        if (!_isActive)
            return false;

        if (!await _databaseManager!.DatabaseExists(cancellationToken))
            return true;
        return !await _databaseManager.TablesExist(["Providers"], cancellationToken);
    }

    public async Task CreateSchema(CancellationToken cancellationToken = default)
    {
        await _databaseManager!.CreateDatabase(cancellationToken);
        var sql = _dbContext!.Database.GenerateCreateScript();
        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public async Task<bool> ShouldSeed(CancellationToken cancellationToken = default)
    {
        if (!_isActive)
            return false;

        return await _databaseManager!.TablesEmpty(["Providers"], cancellationToken);
    }

    public async Task SeedData(CancellationToken cancellationToken = default)
    {
        var providers = await _readOnlyProviderRepository!.GetAll(cancellationToken);
        var providerCatalogs = new List<ProviderCatalog>();
        foreach (var provider in providers)
        {
            var providerModule = await _providerManager!.GetProviderModule(provider.Area, provider.ModuleType, cancellationToken) ??
                throw new InvalidOperationException($"Provider module '{provider.ModuleType}' for area '{provider.Area}' not found.");

            var providerCatalog = new ProviderCatalog(providerModule, provider.Name, provider.IsActive, provider.Options);
            providerCatalogs.Add(providerCatalog);
        }

        await _providerRepository!.AddMany(providers, cancellationToken);
    }
}

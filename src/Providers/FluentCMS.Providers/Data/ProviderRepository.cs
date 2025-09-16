using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers.Data;

internal class ProviderRepository(ProviderDbContext dbContext) : IProviderRepository
{
    public Task Activate(string area, string name, CancellationToken cancellationToken = default)
    {
        var provider = dbContext.Providers.FirstOrDefault(p => p.Area == area && p.Name == name)
            ?? throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        provider.IsActive = true;
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task Add(string area, string name, string moduleTypeName, string options, bool isActive, string displayName, CancellationToken cancellationToken = default)
    {
        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            Area = area,
            Name = name,
            ModuleType = moduleTypeName,
            Options = options,
            IsActive = isActive,
            DisplayName = displayName
        };
        dbContext.Providers.Add(provider);
        return dbContext.SaveChangesAsync(cancellationToken);

    }

    public Task Deactivate(string area, string name, CancellationToken cancellationToken = default)
    {
        var provider = dbContext.Providers.FirstOrDefault(p => p.Area == area && p.Name == name)
            ?? throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        provider.IsActive = false;
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IEnumerable<Provider>> GetAll(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(dbContext.Providers.AsEnumerable());
    }

    public async Task Remove(Provider provider, CancellationToken cancellationToken = default)
    {
        dbContext.Providers.Remove(provider);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task Remove(string area, string name, CancellationToken cancellationToken = default)
    {
        var provider = dbContext.Providers.FirstOrDefault(p => p.Area == area && p.Name == name)
            ?? throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        dbContext.Providers.Remove(provider);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateOptions(string area, string name, string options, CancellationToken cancellationToken = default)
    {
        var provider = dbContext.Providers.FirstOrDefault(p => p.Area == area && p.Name == name)
            ?? throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        provider.Options = options;
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

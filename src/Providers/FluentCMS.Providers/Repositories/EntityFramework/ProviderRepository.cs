using FluentCMS.Providers.Repositories.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Providers.Repositories.EntityFramework;

internal class ProviderRepository(ProviderDbContext dbContext, ILogger<ProviderRepository> logger) : Repository<Provider, ProviderDbContext>(dbContext, logger), IProviderRepository
{
    public async Task Activate(string area, string name, CancellationToken cancellationToken = default)
    {
        var provider = Context.Providers.FirstOrDefault(p => p.Area == area && p.Name == name)
            ?? throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        provider.IsActive = true;
        await Update(provider, cancellationToken);
    }

    public async Task Add(string area, string name, string moduleTypeName, string options, bool isActive, string displayName, CancellationToken cancellationToken = default)
    {
        var provider = new Provider
        {
            Area = area,
            Name = name,
            ModuleType = moduleTypeName,
            Options = options,
            IsActive = isActive,
            DisplayName = displayName
        };
        await Add(provider, cancellationToken);
    }

    public async Task Deactivate(string area, string name, CancellationToken cancellationToken = default)
    {
        var provider = Context.Providers.FirstOrDefault(p => p.Area == area && p.Name == name)
            ?? throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        provider.IsActive = false;
        await Update(provider, cancellationToken);
    }

    public async Task Remove(string area, string name, CancellationToken cancellationToken = default)
    {
        var provider = Context.Providers.FirstOrDefault(p => p.Area == area && p.Name == name)
            ?? throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        Context.Providers.Remove(provider);
        await Remove(provider.Id, cancellationToken);
    }

    public async Task UpdateOptions(string area, string name, string options, CancellationToken cancellationToken = default)
    {
        var provider = Context.Providers.FirstOrDefault(p => p.Area == area && p.Name == name)
            ?? throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        provider.Options = options;
        await Update(provider, cancellationToken);
    }
}

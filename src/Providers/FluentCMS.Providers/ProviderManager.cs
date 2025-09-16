using FluentCMS.Providers.Abstractions;
using FluentCMS.Providers.Repositories.Abstractions;

namespace FluentCMS.Providers;

public interface IProviderManager
{
    Task<ProviderCatalog> Add(ProviderCatalog providerCatalog, CancellationToken cancellationToken = default!);
    Task<IProviderModule?> GetProviderModule(string area, string typeName, CancellationToken cancellationToken = default!);
    Task<ProviderCatalog?> GetActiveByArea(string area, CancellationToken cancellationToken = default);
}

internal sealed class ProviderManager(IProviderRepository repository, ProviderCatalogCache providerCatalogCache) : IProviderManager
{
    public async Task<ProviderCatalog> Add(ProviderCatalog providerCatalog, CancellationToken cancellationToken = default)
    {
        var module = providerCatalog.Module;
        await repository.Add(module.Area, providerCatalog.Name, module.ProviderType.FullName!, "{}", providerCatalog.Active, module.DisplayName, cancellationToken);
        providerCatalogCache.AddCatalog(providerCatalog);

        if (providerCatalog.Active)
        {
            var activeCatalog = providerCatalogCache.GetActiveCatalog(module.Area);
            if (activeCatalog != null)
            {
                activeCatalog.Active = false;
                await repository.Deactivate(activeCatalog.Module.Area, activeCatalog.Name, cancellationToken);
                providerCatalogCache.Deactivate(activeCatalog.Module.Area, activeCatalog.Name);
                providerCatalogCache.Activate(providerCatalog.Module.Area, providerCatalog.Name);
            }
        }
        return providerCatalogCache.GetCatalog(module.Area, providerCatalog.Name) ??
            throw new InvalidOperationException("Failed to add provider catalog.");
    }

    public Task<ProviderCatalog?> GetActiveByArea(string area, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(providerCatalogCache.GetActiveCatalog(area));
    }

    public Task<IProviderModule?> GetProviderModule(string area, string typeName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(providerCatalogCache.GetRegisteredModule(area, typeName));
    }

    public async Task Remove(string area, string name, CancellationToken cancellationToken = default)
    {
        _ = providerCatalogCache.GetCatalog(area, name) ?? throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        await repository.Remove(area, name, cancellationToken);
        providerCatalogCache.RemoveCatalog(area, name);
    }
}

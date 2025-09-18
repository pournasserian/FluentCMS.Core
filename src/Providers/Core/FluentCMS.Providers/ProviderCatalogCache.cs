using System.Collections.Concurrent;

namespace FluentCMS.Providers;

internal sealed class ProviderCatalogCache
{
    private readonly ConcurrentDictionary<string, ProviderCatalog> _catalogsByKey = new();
    private readonly ConcurrentDictionary<string, List<ProviderCatalog>> _catalogsByArea = new();
    private readonly ConcurrentDictionary<string, ProviderCatalog> _activeCatalogs = new();
    private bool _isInitialized = false;
    private readonly Lock _lock = new();

    public bool IsInitialized => _isInitialized;

    public void Initialize(IEnumerable<ProviderCatalog> catalogs)
    {
        lock (_lock)
        {
            if (_isInitialized)
                throw new InvalidOperationException("ProviderCatalogCache is already initialized.");

            foreach (var catalog in catalogs)
            {
                AddCatalog(catalog);
            }
            _isInitialized = true;
        }
    }

    public void Reload(IEnumerable<ProviderCatalog> catalogs)
    {
        lock (_lock)
        {
            Clear();
            Initialize(catalogs);
        }
    }

    public void Clear()
    {
        _catalogsByKey.Clear();
        _catalogsByArea.Clear();
        _activeCatalogs.Clear();
        _isInitialized = false;
    }

    public void AddCatalog(ProviderCatalog providerCatalog)
    {
        lock (_lock)
        {
            var area = providerCatalog.Module.Area;
            var key = $"{area}:{providerCatalog.Name}";

            _catalogsByKey.TryAdd(key, providerCatalog);

            if (providerCatalog.Active)
                _activeCatalogs[area] = providerCatalog;

            _catalogsByArea.AddOrUpdate(area,
                [providerCatalog],
                (area, existing) =>
                {
                    existing.Add(providerCatalog);
                    return existing;
                });
        }
    }

    public ProviderCatalog? GetActiveCatalog(string area)
    {
        _activeCatalogs.TryGetValue(area, out var catalog);
        return catalog;
    }

    public ProviderCatalog? GetCatalog(string area, string providerName)
    {
        var key = $"{area}:{providerName}";
        _catalogsByKey.TryGetValue(key, out var catalog);
        return catalog;
    }

}

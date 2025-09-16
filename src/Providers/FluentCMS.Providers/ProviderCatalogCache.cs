using FluentCMS.Providers.Abstractions;
using System.Collections.Concurrent;

namespace FluentCMS.Providers;

internal sealed class ProviderCatalogCache
{
    #region Module

    // Area -> (TypeName -> IProviderModule)
    private readonly Dictionary<string, Dictionary<string, IProviderModule>> registeredModules = [];

    // Area -> InterfaceType)
    private readonly Dictionary<string, Type> registeredInterfaces = [];

    public ProviderCatalogCache(IEnumerable<IProviderModule> modules)
    {
        foreach (var module in modules)
        {
            if (!registeredInterfaces.TryGetValue(module.Area, out Type? valueType))
            {
                registeredInterfaces[module.Area] = module.InterfaceType;
            }
            else if (valueType != module.InterfaceType)
            {
                throw new InvalidOperationException($"Multiple interface types registered for area '{module.Area}'.");
            }

            if (!registeredModules.TryGetValue(module.Area, out Dictionary<string, IProviderModule>? value))
            {
                value = new Dictionary<string, IProviderModule>(StringComparer.OrdinalIgnoreCase);
                registeredModules[module.Area] = value;
            }
            var moduleType = module.GetType().FullName ??
                throw new InvalidOperationException("Provider type must have a full name.");

            value[moduleType] = module;
        }
    }
    public IEnumerable<IProviderModule> GetRegisteredModules()
    {
        return registeredModules.Values.SelectMany(dict => dict.Values);
    }

    public IReadOnlyDictionary<string, Type> GetRegisteredInterfaceTypes()
    {
        return registeredInterfaces.AsReadOnly();
    }

    public IProviderModule? GetRegisteredModule(string area, string typeName)
    {
        if (registeredModules.TryGetValue(area, out var areaModules) &&
            areaModules.TryGetValue(typeName, out var module))
        {
            return module;
        }
        return null;
    }

    #endregion

    #region ProviderCatalog

    private readonly ConcurrentDictionary<string, ProviderCatalog> _catalogsByKey = new();
    private readonly ConcurrentDictionary<string, List<ProviderCatalog>> _catalogsByArea = new();
    private readonly ConcurrentDictionary<string, ProviderCatalog> _activeCatalogs = new();

    public void AddCatalog(ProviderCatalog providerCatalog) 
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

    public void RemoveCatalog(string area, string providerName)
    {
        var key = $"{area}:{providerName}";
        if (_catalogsByKey.TryRemove(key, out var catalog))
        {
            if (catalog.Active)
                _activeCatalogs.TryRemove(area, out _);
            if (_catalogsByArea.TryGetValue(area, out var list))
            {
                list.RemoveAll(c => c.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                if (list.Count == 0)
                    _catalogsByArea.TryRemove(area, out _);
            }
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

    public void Activate(string area, string name)
    {
        var key = $"{area}:{name}";
        if (_catalogsByKey.TryGetValue(key, out var catalog))
        {
            if (catalog.Active)
                return; // already active
            // Deactivate current active
            if (_activeCatalogs.TryGetValue(area, out var currentActive))
            {
                currentActive.Active = false;
            }
            // Activate new
            catalog.Active = true;
            _activeCatalogs[area] = catalog;
        }
        else
        {
            throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        }
    }

    public void Deactivate(string area, string name)
    {
        var key = $"{area}:{name}";
        if (_catalogsByKey.TryGetValue(key, out var catalog))
        {
            if (!catalog.Active)
                return; // already inactive
            catalog.Active = false;
            _activeCatalogs.TryRemove(area, out _);
        }
        else
        {
            throw new InvalidOperationException($"Provider '{name}' in area '{area}' not found.");
        }
    }
    #endregion

}
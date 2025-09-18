using FluentCMS.Providers.Abstractions;

namespace FluentCMS.Providers;

internal sealed class ProviderModuleCatalogCache
{
    // Area -> (TypeName -> IProviderModule)
    private readonly Dictionary<string, Dictionary<string, IProviderModule>> registeredModules = [];

    // Area -> InterfaceType)
    private readonly Dictionary<string, Type> registeredInterfaces = [];

    public ProviderModuleCatalogCache(IEnumerable<IProviderModule> modules)
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

}
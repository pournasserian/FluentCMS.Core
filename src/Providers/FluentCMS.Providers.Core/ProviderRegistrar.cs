namespace FluentCMS.Providers;

/// <summary>
/// Service for registering providers based on configuration
/// </summary>
public class ProviderRegistrar(IServiceCollection services, IConfiguration configuration)
{
    /// <summary>
    /// Registers all providers based on configuration
    /// </summary>
    public void RegisterProviders()
    {
        // Read provider configurations from appsettings
        var activeProviders = configuration.GetSection("Providers").Get<Dictionary<string, string>>() ?? [];

        // Get all provider instance configurations (everything except "Providers" section)
        var allSections = configuration.GetChildren()
            .Where(section => section.Key != "Providers")
            .ToDictionary(section => section.Key, section => section);

        // Group provider instances by their interface type
        var providersByInterface = GroupProvidersByInterface(allSections);

        // Register each provider type
        foreach (var interfaceGroup in providersByInterface)
        {
            RegisterProvidersForInterface(interfaceGroup.Key, interfaceGroup.Value, activeProviders);
        }
    }

    private Dictionary<Type, Dictionary<string, IConfigurationSection>> GroupProvidersByInterface(
        Dictionary<string, IConfigurationSection> allSections)
    {
        var result = new Dictionary<Type, Dictionary<string, IConfigurationSection>>();

        foreach (var section in allSections)
        {
            var implementationType = section.Value["ImplementationType"];
            if (string.IsNullOrEmpty(implementationType))
                continue;

            var type = Type.GetType(implementationType);
            if (type == null)
                continue;

            // Find the provider interface this type implements
            var providerInterface = FindProviderInterface(type);
            if (providerInterface == null)
                continue;

            if (!result.ContainsKey(providerInterface))
                result[providerInterface] = new Dictionary<string, IConfigurationSection>();

            result[providerInterface][section.Key] = section.Value;
        }

        return result;
    }

    private Type? FindProviderInterface(Type implementationType)
    {
        // Look for interfaces that end with "Provider" (e.g., ICacheProvider, IEmailProvider)
        return implementationType.GetInterfaces()
            .FirstOrDefault(i => i.Name.EndsWith("Provider") && i.IsInterface);
    }

    private void RegisterProvidersForInterface(
        Type interfaceType,
        Dictionary<string, IConfigurationSection> providers,
        Dictionary<string, string> activeProviders)
    {
        var activeProviderName = GetActiveProviderName(interfaceType, activeProviders);

        // Register each provider instance with keyed services
        foreach (var provider in providers)
        {
            RegisterProviderInstance(interfaceType, provider.Key, provider.Value);
        }

        // Register the factory for this provider type
        RegisterProviderFactory(interfaceType, providers.Keys, activeProviderName);

        // Register the active provider for direct injection
        RegisterActiveProvider(interfaceType, activeProviderName);
    }

    private string? GetActiveProviderName(Type interfaceType, Dictionary<string, string> activeProviders)
    {
        // Try to find active provider by interface name (e.g., "Cache" for ICacheProvider)
        var providerTypeName = interfaceType.Name.Replace("Provider", "").Replace("I", "");
        activeProviders.TryGetValue(providerTypeName, out var activeProviderName);
        return activeProviderName;
    }

    private void RegisterProviderInstance(Type interfaceType, string providerName, IConfigurationSection config)
    {
        var implementationType = config["ImplementationType"];
        if (string.IsNullOrEmpty(implementationType))
            return;

        var type = Type.GetType(implementationType);
        if (type == null)
            throw new InvalidOperationException($"Could not load type: {implementationType}");

        // Register the provider with keyed service
        services.AddKeyedScoped(interfaceType, providerName, (serviceProvider, key) =>
        {
            return CreateProviderInstance(type, config, serviceProvider);
        });
    }

    private object CreateProviderInstance(Type implementationType, IConfigurationSection config, IServiceProvider serviceProvider)
    {
        // Create options type for this provider
        var optionsType = typeof(Dictionary<string, object>);
        var options = config.Get<Dictionary<string, object>>() ?? new Dictionary<string, object>();

        // Try to find a constructor that accepts IServiceProvider or options
        var constructors = implementationType.GetConstructors();

        // First try constructor with IServiceProvider
        var serviceProviderConstructor = constructors.FirstOrDefault(c =>
            c.GetParameters().Length == 1 &&
            c.GetParameters()[0].ParameterType == typeof(IServiceProvider));

        if (serviceProviderConstructor != null)
        {
            return Activator.CreateInstance(implementationType, serviceProvider)!;
        }

        // Try parameterless constructor
        var parameterlessConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
        if (parameterlessConstructor != null)
        {
            return Activator.CreateInstance(implementationType)!;
        }

        throw new InvalidOperationException($"No suitable constructor found for type: {implementationType.Name}");
    }

    private void RegisterProviderFactory(Type interfaceType, IEnumerable<string> providerNames, string? activeProviderName)
    {
        var factoryType = typeof(IProviderFactory<>).MakeGenericType(interfaceType);
        var factoryImplementationType = typeof(ProviderFactory<>).MakeGenericType(interfaceType);
        var optionsType = typeof(ProviderFactoryOptions<>).MakeGenericType(interfaceType);

        // Configure factory options
        services.Configure(optionsType, (object options) =>
        {
            var activeProviderProperty = optionsType.GetProperty("ActiveProviderName");
            var providersProperty = optionsType.GetProperty("Providers");

            activeProviderProperty?.SetValue(options, activeProviderName ?? string.Empty);

            var providersDict = new Dictionary<string, object>();
            foreach (var providerName in providerNames)
            {
                providersDict[providerName] = new object();
            }
            providersProperty?.SetValue(options, providersDict);
        });

        // Register the factory
        services.AddScoped(factoryType, factoryImplementationType);
    }

    private void RegisterActiveProvider(Type interfaceType, string? activeProviderName)
    {
        if (string.IsNullOrEmpty(activeProviderName))
            return;

        // Register the interface to resolve to the active provider
        services.AddScoped(interfaceType, serviceProvider =>
        {
            var factoryType = typeof(IProviderFactory<>).MakeGenericType(interfaceType);
            var factory = serviceProvider.GetRequiredService(factoryType);
            var getActiveProviderMethod = factoryType.GetMethod("GetActiveProvider");
            return getActiveProviderMethod!.Invoke(factory, null)!;
        });
    }
}

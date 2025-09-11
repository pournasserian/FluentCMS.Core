namespace FluentCMS.Providers;

/// <summary>
/// Default implementation of provider factory
/// </summary>
/// <typeparam name="T">The provider interface type</typeparam>
public class ProviderFactory<T> : IProviderFactory<T> where T : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ProviderFactoryOptions<T>> _options;
    private readonly Dictionary<string, T> _providers = [];

    public ProviderFactory(IServiceProvider serviceProvider, IOptionsMonitor<ProviderFactoryOptions<T>> options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        InitializeProviders();
    }

    public T GetActiveProvider()
    {
        var options = _options.CurrentValue;
        if (string.IsNullOrEmpty(options.ActiveProviderName))
        {
            throw new InvalidOperationException($"No active provider configured for type {typeof(T).Name}");
        }

        return GetProvider(options.ActiveProviderName);
    }

    public T GetProvider(string name)
    {
        if (!_providers.TryGetValue(name, out var provider))
        {
            throw new InvalidOperationException($"Provider '{name}' not found for type {typeof(T).Name}");
        }

        return provider;
    }

    public IReadOnlyDictionary<string, T> GetAllProviders()
    {
        return _providers.AsReadOnly();
    }

    public bool HasProvider(string name)
    {
        return _providers.ContainsKey(name);
    }

    private void InitializeProviders()
    {
        var options = _options.CurrentValue;
        foreach (var providerConfig in options.Providers)
        {
            var provider = _serviceProvider.GetRequiredKeyedService<T>(providerConfig.Key);
            _providers[providerConfig.Key] = provider;
        }
    }
}

/// <summary>
/// Options for provider factory configuration
/// </summary>
/// <typeparam name="T">The provider interface type</typeparam>
public class ProviderFactoryOptions<T> where T : class
{
    /// <summary>
    /// The name of the active provider
    /// </summary>
    public string ActiveProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary of provider configurations
    /// </summary>
    public Dictionary<string, object> Providers { get; set; } = [];
}

namespace FluentCMS.Providers.DI;

/// <summary>
/// Provides a mechanism for modifying service registrations at runtime.
/// </summary>
internal class DynamicServiceProvider : IDisposable
{
    private readonly ILogger<DynamicServiceProvider> _logger;
    private readonly Lock _lock = new();
    private readonly List<ServiceDescriptor> _baseServiceDescriptors;

    private IServiceCollection _currentServiceCollection;
    private IServiceProvider? _currentServiceProvider;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicServiceProvider"/> class.
    /// </summary>
    /// <param name="baseServiceCollection">The base service collection to modify.</param>
    /// <param name="logger">The logger.</param>
    public DynamicServiceProvider(IServiceCollection baseServiceCollection, ILogger<DynamicServiceProvider> logger)
    {
        _logger = logger;
        
        // Make a copy of the original service descriptors
        _baseServiceDescriptors = baseServiceCollection.ToList();
        
        // Create a new service collection with the base descriptors
        _currentServiceCollection = new ServiceCollection();
        foreach (var descriptor in _baseServiceDescriptors)
        {
            _currentServiceCollection.Add(descriptor);
        }
        
        // Build the initial service provider
        _currentServiceProvider = _currentServiceCollection.BuildServiceProvider();
    }
    
    /// <summary>
    /// Gets the current service provider.
    /// </summary>
    public IServiceProvider ServiceProvider
    {
        get
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _currentServiceProvider!;
            }
        }
    }
    
    /// <summary>
    /// Registers a provider implementation as a singleton service.
    /// </summary>
    /// <typeparam name="TProvider">The provider interface type.</typeparam>
    /// <param name="providerType">The provider implementation type.</param>
    /// <param name="instance">The provider instance.</param>
    /// <returns>True if the service was registered, false if it already exists.</returns>
    public bool RegisterProviderSingleton<TProvider>(Type providerType, TProvider instance) 
        where TProvider : class, IProvider
    {
        lock (_lock)
        {
            ThrowIfDisposed();

            try
            {
                // Check if the service already exists
                var existingDescriptor = _currentServiceCollection.FirstOrDefault(
                    d => d.ServiceType == typeof(TProvider) && d.ImplementationType == providerType);
                
                if (existingDescriptor != null)
                {
                    _logger.LogWarning("Provider {ProviderType} is already registered for {ServiceType}",
                        providerType.Name, typeof(TProvider).Name);
                    return false;
                }
                
                // Create a new service collection with the updated registrations
                var newServiceCollection = new ServiceCollection();
                
                // Copy the base service descriptors
                foreach (var descriptor in _baseServiceDescriptors)
                {
                    newServiceCollection.Add(descriptor);
                }
                
                // Add the current dynamic registrations (excluding the service type we're adding)
                foreach (var descriptor in _currentServiceCollection)
                {
                    if (!_baseServiceDescriptors.Contains(descriptor) && descriptor.ServiceType != typeof(TProvider))
                    {
                        newServiceCollection.Add(descriptor);
                    }
                }
                
                // Add the new service registration
                newServiceCollection.AddSingleton(typeof(TProvider), instance);
                
                // Build a new service provider
                var newServiceProvider = newServiceCollection.BuildServiceProvider();
                
                // Replace the current service collection and provider
                var oldServiceProvider = _currentServiceProvider;
                _currentServiceCollection = newServiceCollection;
                _currentServiceProvider = newServiceProvider;
                
                // Dispose the old service provider
                if (oldServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                
                _logger.LogInformation("Registered provider {ProviderType} for {ServiceType}",
                    providerType.Name, typeof(TProvider).Name);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register provider {ProviderType} for {ServiceType}",
                    providerType.Name, typeof(TProvider).Name);
                throw;
            }
        }
    }
    
    /// <summary>
    /// Unregisters a provider implementation.
    /// </summary>
    /// <typeparam name="TProvider">The provider interface type.</typeparam>
    /// <returns>True if the service was unregistered, false if it was not found.</returns>
    public bool UnregisterProvider<TProvider>() where TProvider : class, IProvider
    {
        lock (_lock)
        {
            ThrowIfDisposed();

            try
            {
                // Check if the service exists
                var existingDescriptor = _currentServiceCollection.FirstOrDefault(
                    d => d.ServiceType == typeof(TProvider));
                
                if (existingDescriptor == null)
                {
                    _logger.LogWarning("Provider {ServiceType} is not registered", typeof(TProvider).Name);
                    return false;
                }
                
                // Create a new service collection without the service
                var newServiceCollection = new ServiceCollection();
                
                // Copy the base service descriptors
                foreach (var descriptor in _baseServiceDescriptors)
                {
                    newServiceCollection.Add(descriptor);
                }
                
                // Add the current dynamic registrations (excluding the service type we're removing)
                foreach (var descriptor in _currentServiceCollection)
                {
                    if (!_baseServiceDescriptors.Contains(descriptor) && descriptor.ServiceType != typeof(TProvider))
                    {
                        newServiceCollection.Add(descriptor);
                    }
                }
                
                // Build a new service provider
                var newServiceProvider = newServiceCollection.BuildServiceProvider();
                
                // Replace the current service collection and provider
                var oldServiceProvider = _currentServiceProvider;
                _currentServiceCollection = newServiceCollection;
                _currentServiceProvider = newServiceProvider;
                
                // Dispose the old service provider
                if (oldServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                
                _logger.LogInformation("Unregistered provider {ServiceType}", typeof(TProvider).Name);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister provider {ServiceType}", typeof(TProvider).Name);
                throw;
            }
        }
    }
    
    /// <summary>
    /// Registers an options instance.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="options">The options instance.</param>
    /// <returns>True if the options were registered, false if they already exist.</returns>
    public bool RegisterOptions<TOptions>(TOptions options) where TOptions : class, new()
    {
        lock (_lock)
        {
            ThrowIfDisposed();

            try
            {
                // Check if the options already exist
                var optionsType = typeof(Microsoft.Extensions.Options.IOptions<TOptions>);
                var existingDescriptor = _currentServiceCollection.FirstOrDefault(
                    d => d.ServiceType == optionsType);
                
                // Create a new service collection with the updated registrations
                var newServiceCollection = new ServiceCollection();
                
                // Copy the base service descriptors
                foreach (var descriptor in _baseServiceDescriptors)
                {
                    newServiceCollection.Add(descriptor);
                }
                
                // Add the current dynamic registrations (excluding the options we're adding)
                foreach (var descriptor in _currentServiceCollection)
                {
                    if (!_baseServiceDescriptors.Contains(descriptor) && descriptor.ServiceType != optionsType)
                    {
                        newServiceCollection.Add(descriptor);
                    }
                }
                
                // Add the new options registration
                newServiceCollection.AddSingleton(options);
                newServiceCollection.AddSingleton(typeof(Microsoft.Extensions.Options.IOptions<TOptions>),
                    provider => new Microsoft.Extensions.Options.OptionsWrapper<TOptions>(options));
                
                // Build a new service provider
                var newServiceProvider = newServiceCollection.BuildServiceProvider();
                
                // Replace the current service collection and provider
                var oldServiceProvider = _currentServiceProvider;
                _currentServiceCollection = newServiceCollection;
                _currentServiceProvider = newServiceProvider;
                
                // Dispose the old service provider
                if (oldServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                
                _logger.LogInformation("Registered options {OptionsType}", typeof(TOptions).Name);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register options {OptionsType}", typeof(TOptions).Name);
                throw;
            }
        }
    }
    
    /// <summary>
    /// Gets a service from the current service provider.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance or null if not found.</returns>
    public T? GetService<T>() where T : class
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            return _currentServiceProvider!.GetService<T>();
        }
    }
    
    /// <summary>
    /// Gets a required service from the current service provider.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">The service is not registered.</exception>
    public T GetRequiredService<T>() where T : class
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            return _currentServiceProvider!.GetRequiredService<T>();
        }
    }
    
    /// <summary>
    /// Disposes the dynamic service provider and all service providers it has created.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }
            
            if (_currentServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _currentServiceProvider = null;
            _isDisposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (!_isDisposed) return;
        throw new ObjectDisposedException(nameof(ProviderAssemblyManager));
    }
}

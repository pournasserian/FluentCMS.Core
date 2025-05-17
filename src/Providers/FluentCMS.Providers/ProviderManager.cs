using FluentCMS.Providers.Data;
using FluentCMS.Providers.Data.Models;
using FluentCMS.Providers.DI;
using FluentCMS.Providers.Loading;
using FluentCMS.Providers.Options;
using Microsoft.Extensions.Options;

namespace FluentCMS.Providers;

/// <summary>
/// Implementation of the provider manager.
/// </summary>
public class ProviderManager : IProviderManager, IDisposable
{
    private readonly IProviderRepository _repository;
    private readonly ProviderAssemblyManager _assemblyManager;
    private readonly DynamicServiceProvider _serviceProvider;
    private readonly ILogger<ProviderManager> _logger;
    private readonly ProviderSystemOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderManager"/> class.
    /// </summary>
    /// <param name="repository">The provider repository.</param>
    /// <param name="assemblyManager">The provider assembly manager.</param>
    /// <param name="serviceProvider">The dynamic service provider.</param>
    /// <param name="options">The provider system options.</param>
    /// <param name="logger">The logger.</param>
    public ProviderManager(
        IProviderRepository repository,
        ProviderAssemblyManager assemblyManager,
        DynamicServiceProvider serviceProvider,
        IOptions<ProviderSystemOptions> options,
        ILogger<ProviderManager> logger)
    {
        _repository = repository;
        _assemblyManager = assemblyManager;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProviderTypeInfo>> GetProviderTypesAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        var providerTypes = await _repository.GetProviderTypesAsync(cancellationToken);
        
        return providerTypes.Select(pt => new ProviderTypeInfo
        {
            Id = pt.Id,
            Name = pt.Name,
            FullTypeName = pt.FullTypeName,
            AssemblyName = pt.AssemblyName,
            CreatedAt = pt.CreatedAt,
            UpdatedAt = pt.UpdatedAt
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProviderImplementationInfo>> GetImplementationsAsync<TProvider>(CancellationToken cancellationToken = default)
        where TProvider : IProvider
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        // Get the provider type
        var providerType = await GetOrCreateProviderTypeAsync<TProvider>(cancellationToken);
        if (providerType == null)
        {
            throw new ProviderNotFoundException($"Provider type {typeof(TProvider).FullName} not found");
        }
        
        // Get implementations for the provider type
        return await GetImplementationsAsync(providerType.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProviderImplementationInfo>> GetImplementationsAsync(string providerTypeId, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        var implementations = await _repository.GetProviderImplementationsByTypeAsync(providerTypeId, cancellationToken);
        
        return implementations.Select(impl => new ProviderImplementationInfo
        {
            Id = impl.Id,
            ProviderTypeId = impl.ProviderTypeId,
            Name = impl.Name,
            Description = impl.Description,
            Version = impl.Version,
            FullTypeName = impl.FullTypeName,
            AssemblyPath = impl.AssemblyPath,
            IsInstalled = impl.IsInstalled,
            IsActive = impl.IsActive,
            HealthStatus = impl.HealthStatus,
            HealthMessage = impl.HealthMessage,
            LastHealthCheckAt = impl.LastHealthCheckAt,
            InstalledAt = impl.InstalledAt,
            ActivatedAt = impl.ActivatedAt,
            UpdatedAt = impl.UpdatedAt
        });
    }

    /// <inheritdoc />
    public async Task<TProvider?> GetActiveProviderAsync<TProvider>(CancellationToken cancellationToken = default)
        where TProvider : IProvider
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        // Get the provider type
        var providerType = await GetOrCreateProviderTypeAsync<TProvider>(cancellationToken);
        if (providerType == null)
        {
            throw new ProviderNotFoundException($"Provider type {typeof(TProvider).FullName} not found");
        }
        
        // Get the active implementation from the repository
        var activeImpl = await _repository.GetActiveProviderImplementationAsync(providerType.Id, cancellationToken);
        if (activeImpl == null)
        {
            return default;
        }
        
        // Get the provider instance from the dynamic service provider
        var provider = _serviceProvider.GetService<TProvider>();
        if (provider == null)
        {
            // Try to load and activate the provider
            await LoadAndActivateProviderAsync<TProvider>(activeImpl, cancellationToken);
            provider = _serviceProvider.GetService<TProvider>();
        }
        
        return provider;
    }

    /// <inheritdoc />
    public async Task<ProviderImplementationInfo?> GetActiveImplementationAsync(string providerTypeId, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        var activeImpl = await _repository.GetActiveProviderImplementationAsync(providerTypeId, cancellationToken);
        if (activeImpl == null)
        {
            return null;
        }
        
        return new ProviderImplementationInfo
        {
            Id = activeImpl.Id,
            ProviderTypeId = activeImpl.ProviderTypeId,
            Name = activeImpl.Name,
            Description = activeImpl.Description,
            Version = activeImpl.Version,
            FullTypeName = activeImpl.FullTypeName,
            AssemblyPath = activeImpl.AssemblyPath,
            IsInstalled = activeImpl.IsInstalled,
            IsActive = activeImpl.IsActive,
            HealthStatus = activeImpl.HealthStatus,
            HealthMessage = activeImpl.HealthMessage,
            LastHealthCheckAt = activeImpl.LastHealthCheckAt,
            InstalledAt = activeImpl.InstalledAt,
            ActivatedAt = activeImpl.ActivatedAt,
            UpdatedAt = activeImpl.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task SetActiveImplementationAsync(string providerTypeId, string implementationId, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        // Get the implementation to activate
        var implementation = await _repository.GetProviderImplementationByIdAsync(implementationId, cancellationToken);
        if (implementation == null)
        {
            throw new ProviderNotFoundException($"Provider implementation with ID {implementationId} not found");
        }
        
        // Verify the implementation belongs to the specified provider type
        if (implementation.ProviderTypeId != providerTypeId)
        {
            throw new ProviderException($"Provider implementation {implementationId} does not belong to provider type {providerTypeId}");
        }
        
        // Get the provider type
        var providerType = await _repository.GetProviderTypeByIdAsync(providerTypeId, cancellationToken);
        if (providerType == null)
        {
            throw new ProviderNotFoundException($"Provider type with ID {providerTypeId} not found");
        }
        
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Get the currently active implementation
            var activeImplementation = await _repository.GetActiveProviderImplementationAsync(providerTypeId, cancellationToken);
            
            // If the implementation is already active, do nothing
            if (activeImplementation != null && activeImplementation.Id == implementationId)
            {
                return;
            }
            
            // Find the provider interface type
            Type providerInterfaceType;
            try
            {
                var assembly = Assembly.Load(providerType.AssemblyName);
                providerInterfaceType = assembly.GetType(providerType.FullTypeName) 
                                       ?? throw new ProviderException($"Provider type {providerType.FullTypeName} not found in assembly {providerType.AssemblyName}");
            }
            catch (Exception ex)
            {
                throw new ProviderException($"Failed to load provider type {providerType.FullTypeName} from assembly {providerType.AssemblyName}", ex);
            }
            
            // If there's an active implementation, deactivate it
            if (activeImplementation != null)
            {
                await DeactivateProviderAsync(activeImplementation, providerInterfaceType, cancellationToken);
            }
            
            // Load and activate the new implementation
            await LoadAndActivateProviderAsync(implementation, providerInterfaceType, cancellationToken);
            
            // Update the active implementation in the repository
            await _repository.SetActiveProviderImplementationAsync(providerTypeId, implementationId, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<T> GetConfigurationAsync<T>(string implementationId, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        // Get the implementation
        var implementation = await _repository.GetProviderImplementationByIdAsync(implementationId, cancellationToken);
        if (implementation == null)
        {
            throw new ProviderNotFoundException($"Provider implementation with ID {implementationId} not found");
        }
        
        // Get the configuration
        var config = await _repository.GetTypedProviderConfigurationAsync<T>(implementationId, cancellationToken);
        
        // If no configuration exists, return a new instance
        return config ?? new T();
    }

    /// <inheritdoc />
    public async Task UpdateConfigurationAsync<T>(string implementationId, T options, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        // Get the implementation
        var implementation = await _repository.GetProviderImplementationByIdAsync(implementationId, cancellationToken);
        if (implementation == null)
        {
            throw new ProviderNotFoundException($"Provider implementation with ID {implementationId} not found");
        }
        
        // Update the configuration
        await _repository.UpdateTypedProviderConfigurationAsync(implementationId, options, cancellationToken);
        
        // If the implementation is active, update the options in the service provider
        if (implementation.IsActive)
        {
            _serviceProvider.RegisterOptions(options);
        }
    }

    /// <inheritdoc />
    public async Task<ProviderImplementationInfo> InstallProviderAsync(string assemblyPath, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Ensure the file exists
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Provider assembly not found at path: {assemblyPath}", assemblyPath);
            }
            
            // Load the assembly
            var assemblyInfo = _assemblyManager.LoadAssembly(assemblyPath);
            
            // Scan for provider interface types
            var interfaceTypes = _assemblyManager.ScanAssemblyForProviderInterfaces(assemblyInfo);
            
            // If no provider interfaces found, throw an exception
            if (!interfaceTypes.Any())
            {
                throw new ProviderLoadException($"No provider interface types found in assembly: {assemblyPath}");
            }
            
            // Register each provider interface type if not already registered
            foreach (var interfaceType in interfaceTypes)
            {
                await GetOrCreateProviderTypeAsync(interfaceType, cancellationToken);
            }
            
            // We'll just install the first valid provider implementation we find
            ProviderImplementation? installedProvider = null;
            
            // Scan for provider implementations
            foreach (var interfaceType in interfaceTypes)
            {
                if (!typeof(IProvider).IsAssignableFrom(interfaceType))
                {
                    continue;
                }
                
                // Get the provider type
                var providerType = await _repository.GetProviderTypeByFullTypeNameAsync(interfaceType.FullName!, cancellationToken);
                if (providerType == null)
                {
                    continue;
                }
                
                // Scan for implementations of this interface
                var implementationTypes = _assemblyManager.ScanAssemblyForProviders(interfaceType, assemblyInfo);
                
                foreach (var implType in implementationTypes)
                {
                    // Skip abstract classes and interfaces
                    if (implType.IsAbstract || implType.IsInterface)
                    {
                        continue;
                    }
                    
                    // Check if this implementation is already installed
                    var existingImpl = await _repository.GetProviderImplementationByFullTypeNameAsync(
                        providerType.Id, implType.FullName!, cancellationToken);
                    
                    if (existingImpl != null)
                    {
                        // Update the existing implementation
                        existingImpl.Name = implType.Name;
                        existingImpl.FullTypeName = implType.FullName!;
                        existingImpl.AssemblyPath = assemblyPath;
                        existingImpl.IsInstalled = true;
                        existingImpl.InstalledAt = DateTimeOffset.UtcNow;
                        existingImpl.UpdatedAt = DateTimeOffset.UtcNow;
                        
                        await _repository.UpdateProviderImplementationAsync(existingImpl, cancellationToken);
                        installedProvider = existingImpl;
                    }
                    else
                    {
                        // Create a new implementation
                        var newImpl = new ProviderImplementation
                        {
                            ProviderTypeId = providerType.Id,
                            Name = implType.Name,
                            Description = implType.Name,
                            Version = assemblyInfo.Assembly.GetName().Version?.ToString() ?? "1.0.0",
                            FullTypeName = implType.FullName!,
                            AssemblyPath = assemblyPath,
                            IsInstalled = true,
                            IsActive = false,
                            HealthStatus = ProviderHealthStatus.Unknown,
                            InstalledAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        };
                        
                        var addedImpl = await _repository.AddProviderImplementationAsync(newImpl, cancellationToken);
                        installedProvider = addedImpl;
                    }
                    
                    // Initialize the provider
                    try
                    {
                        var provider = CreateProviderInstance(implType, installedProvider.Id);
                        
                        if (provider is IProviderLifecycle lifecycle)
                        {
                            await lifecycle.Initialize(cancellationToken);
                        }
                        
                        // Update health status
                        if (provider is IProviderHealth health)
                        {
                            var (status, message) = await health.GetStatus(cancellationToken);
                            await _repository.UpdateProviderHealthStatusAsync(installedProvider.Id, status, message, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to initialize provider: {ProviderName}", implType.FullName);
                        await _repository.UpdateProviderHealthStatusAsync(installedProvider.Id, ProviderHealthStatus.Unhealthy, ex.Message, cancellationToken);
                    }
                }
            }
            
            if (installedProvider == null)
            {
                throw new ProviderLoadException($"No valid provider implementations found in assembly: {assemblyPath}");
            }
            
            return new ProviderImplementationInfo
            {
                Id = installedProvider.Id,
                ProviderTypeId = installedProvider.ProviderTypeId,
                Name = installedProvider.Name,
                Description = installedProvider.Description,
                Version = installedProvider.Version,
                FullTypeName = installedProvider.FullTypeName,
                AssemblyPath = installedProvider.AssemblyPath,
                IsInstalled = installedProvider.IsInstalled,
                IsActive = installedProvider.IsActive,
                HealthStatus = installedProvider.HealthStatus,
                HealthMessage = installedProvider.HealthMessage,
                LastHealthCheckAt = installedProvider.LastHealthCheckAt,
                InstalledAt = installedProvider.InstalledAt,
                ActivatedAt = installedProvider.ActivatedAt,
                UpdatedAt = installedProvider.UpdatedAt
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UninstallProviderAsync(string implementationId, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Get the implementation
            var implementation = await _repository.GetProviderImplementationByIdAsync(implementationId, cancellationToken);
            if (implementation == null)
            {
                throw new ProviderNotFoundException($"Provider implementation with ID {implementationId} not found");
            }
            
            // Don't allow uninstallation of active providers
            if (implementation.IsActive)
            {
                throw new ProviderException("Cannot uninstall an active provider. Deactivate it first.");
            }
            
            // If the implementation is from an assembly that contains other implementations,
            // just mark it as uninstalled but don't unload the assembly
            var assembly = _assemblyManager.GetAssembly(implementation.AssemblyPath);
            if (assembly != null)
            {
                var otherImplementations = await _repository.GetProviderImplementationsAsync(cancellationToken);
                var sameAssemblyImplementations = otherImplementations
                    .Where(i => i.Id != implementationId && i.AssemblyPath == implementation.AssemblyPath && i.IsInstalled)
                    .ToList();
                
                if (sameAssemblyImplementations.Any())
                {
                    // Just mark it as uninstalled
                    implementation.IsInstalled = false;
                    implementation.UpdatedAt = DateTimeOffset.UtcNow;
                    await _repository.UpdateProviderImplementationAsync(implementation, cancellationToken);
                    return;
                }
                
                // Get the provider type
                var providerType = await _repository.GetProviderTypeByIdAsync(implementation.ProviderTypeId, cancellationToken);
                if (providerType != null)
                {
                    // Find the provider interface type
                    Type providerInterfaceType;
                    try
                    {
                        var interfaceAssembly = Assembly.Load(providerType.AssemblyName);
                        providerInterfaceType = interfaceAssembly.GetType(providerType.FullTypeName)
                                              ?? throw new ProviderException($"Provider type {providerType.FullTypeName} not found in assembly {providerType.AssemblyName}");
                    }
                    catch (Exception ex)
                    {
                        throw new ProviderException($"Failed to load provider type {providerType.FullTypeName} from assembly {providerType.AssemblyName}", ex);
                    }
                    
                    // Create an instance to call the uninstall method
                    try
                    {
                        // Find the implementation type
                        var implType = assembly.Assembly.GetType(implementation.FullTypeName);
                        if (implType != null)
                        {
                            var provider = CreateProviderInstance(implType, implementation.Id);
                            
                            if (provider is IProviderLifecycle lifecycle)
                            {
                                await lifecycle.Uninstall(cancellationToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during provider uninstall: {ProviderName}", implementation.FullTypeName);
                    }
                }
                
                // Unload the assembly
                _assemblyManager.UnloadAssembly(implementation.AssemblyPath);
            }
            
            // Delete the implementation from the database
            await _repository.DeleteProviderImplementationAsync(implementationId, cancellationToken);
            
            // Delete any configuration
            await _repository.DeleteProviderConfigurationAsync(implementationId, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<(ProviderHealthStatus Status, string Message)> CheckHealthAsync(string implementationId, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        // Get the implementation
        var implementation = await _repository.GetProviderImplementationByIdAsync(implementationId, cancellationToken);
        if (implementation == null)
        {
            throw new ProviderNotFoundException($"Provider implementation with ID {implementationId} not found");
        }
        
        try
        {
            // Load the assembly
            var assembly = _assemblyManager.GetAssembly(implementation.AssemblyPath);
            if (assembly == null)
            {
                assembly = _assemblyManager.LoadAssembly(implementation.AssemblyPath);
            }
            
            // Find the implementation type
            var implType = assembly.Assembly.GetType(implementation.FullTypeName);
            if (implType == null)
            {
                throw new ProviderException($"Implementation type {implementation.FullTypeName} not found in assembly {implementation.AssemblyPath}");
            }
            
            // Create an instance
            var provider = CreateProviderInstance(implType, implementation.Id);
            
            // Check health
            if (provider is IProviderHealth health)
            {
                var (status, message) = await health.GetStatus(cancellationToken);
                
                // Update the health status in the database
                await _repository.UpdateProviderHealthStatusAsync(implementationId, status, message, cancellationToken);
                
                return (status, message);
            }
            
            // If the provider doesn't implement IProviderHealth, assume it's healthy
            var defaultStatus = ProviderHealthStatus.Healthy;
            var defaultMessage = "Provider is healthy";
            
            await _repository.UpdateProviderHealthStatusAsync(implementationId, defaultStatus, defaultMessage, cancellationToken);
            
            return (defaultStatus, defaultMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check health for provider: {ProviderName}", implementation.FullTypeName);
            
            // Update the health status in the database
            await _repository.UpdateProviderHealthStatusAsync(implementationId, ProviderHealthStatus.Unhealthy, ex.Message, cancellationToken);
            
            return (ProviderHealthStatus.Unhealthy, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task RefreshProviderRegistryAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProviderManager));
        }
        
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Scan the provider directory for assemblies
            var providerDirectory = Path.GetFullPath(_options.ProviderDirectory);
            if (!Directory.Exists(providerDirectory))
            {
                Directory.CreateDirectory(providerDirectory);
            }
            
            // Get all DLL files in the provider directory
            var dllFiles = Directory.GetFiles(providerDirectory, "*.dll", SearchOption.AllDirectories);
            
            // Load each assembly and scan for providers
            foreach (var dllFile in dllFiles)
            {
                try
                {
                    await InstallProviderAsync(dllFile, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to install provider from assembly: {AssemblyPath}", dllFile);
                }
            }
            
            // Get all provider implementations
            var implementations = await _repository.GetProviderImplementationsAsync(cancellationToken);
            
            // Check if each implementation's assembly still exists
            foreach (var implementation in implementations)
            {
                if (!File.Exists(implementation.AssemblyPath))
                {
                    if (implementation.IsActive)
                    {
                        _logger.LogWarning("Active provider's assembly not found: {ProviderName} at {AssemblyPath}", 
                            implementation.FullTypeName, implementation.AssemblyPath);
                    }
                    else if (implementation.IsInstalled)
                    {
                        _logger.LogInformation("Marking provider as uninstalled because assembly not found: {ProviderName} at {AssemblyPath}", 
                            implementation.FullTypeName, implementation.AssemblyPath);
                        
                        implementation.IsInstalled = false;
                        implementation.UpdatedAt = DateTimeOffset.UtcNow;
                        await _repository.UpdateProviderImplementationAsync(implementation, cancellationToken);
                    }
                }
                else if (implementation.IsInstalled)
                {
                    // Check the provider's health
                    try
                    {
                        await CheckHealthAsync(implementation.Id, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to check health for provider: {ProviderName}", implementation.FullTypeName);
                    }
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets or creates a provider type for the specified interface type.
    /// </summary>
    /// <typeparam name="TProvider">The provider interface type.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The provider type, or null if it could not be created.</returns>
    private async Task<ProviderType?> GetOrCreateProviderTypeAsync<TProvider>(CancellationToken cancellationToken = default)
        where TProvider : IProvider
    {
        return await GetOrCreateProviderTypeAsync(typeof(TProvider), cancellationToken);
    }

    /// <summary>
    /// Gets or creates a provider type for the specified interface type.
    /// </summary>
    /// <param name="interfaceType">The provider interface type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The provider type, or null if it could not be created.</returns>
    private async Task<ProviderType?> GetOrCreateProviderTypeAsync(Type interfaceType, CancellationToken cancellationToken = default)
    {
        // Check if the interface type is a provider interface
        if (!typeof(IProvider).IsAssignableFrom(interfaceType) || !interfaceType.IsInterface)
        {
            return null;
        }
        
        // Get the provider type by full type name
        var providerType = await _repository.GetProviderTypeByFullTypeNameAsync(interfaceType.FullName!, cancellationToken);
        
        // If it exists, return it
        if (providerType != null)
        {
            return providerType;
        }
        
        // Create a new provider type
        var newProviderType = new ProviderType
        {
            Name = interfaceType.Name,
            DisplayName = interfaceType.Name.Replace("IProvider", "").Replace("Provider", ""),
            FullTypeName = interfaceType.FullName!,
            AssemblyName = interfaceType.Assembly.FullName!
        };
        
        // Add it to the repository
        return await _repository.AddProviderTypeAsync(newProviderType, cancellationToken);
    }

    /// <summary>
    /// Creates an instance of the specified provider type.
    /// </summary>
    /// <param name="providerType">The provider implementation type.</param>
    /// <param name="providerId">The provider implementation ID.</param>
    /// <returns>The provider instance.</returns>
    private IProvider CreateProviderInstance(Type providerType, string providerId)
    {
        // Create an instance of the provider
        var provider = Activator.CreateInstance(providerType) as IProvider 
                    ?? throw new ProviderException($"Failed to create instance of provider type: {providerType.FullName}");
        
        // Set the ID if it's a ProviderBase
        if (provider is ProviderBase providerBase)
        {
            typeof(ProviderBase)
                .GetProperty(nameof(ProviderBase.Id))!
                .SetValue(providerBase, providerId);
        }
        
        return provider;
    }

    /// <summary>
    /// Loads and activates a provider.
    /// </summary>
    /// <typeparam name="TProvider">The provider interface type.</typeparam>
    /// <param name="implementation">The provider implementation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activated provider instance.</returns>
    private async Task<TProvider> LoadAndActivateProviderAsync<TProvider>(
        ProviderImplementation implementation, 
        CancellationToken cancellationToken = default)
        where TProvider : IProvider
    {
        return (TProvider)await LoadAndActivateProviderAsync(implementation, typeof(TProvider), cancellationToken);
    }

    /// <summary>
    /// Loads and activates a provider.
    /// </summary>
    /// <param name="implementation">The provider implementation.</param>
    /// <param name="providerInterfaceType">The provider interface type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activated provider instance.</returns>
    private async Task<IProvider> LoadAndActivateProviderAsync(
        ProviderImplementation implementation, 
        Type providerInterfaceType, 
        CancellationToken cancellationToken = default)
    {
        // Load the assembly
        var assemblyInfo = _assemblyManager.GetAssembly(implementation.AssemblyPath);
        if (assemblyInfo == null)
        {
            assemblyInfo = _assemblyManager.LoadAssembly(implementation.AssemblyPath);
        }
        
        // Find the implementation type
        var implType = assemblyInfo.Assembly.GetType(implementation.FullTypeName);
        if (implType == null)
        {
            throw new ProviderException($"Implementation type {implementation.FullTypeName} not found in assembly {implementation.AssemblyPath}");
        }
        
        // Create an instance of the provider
        var provider = CreateProviderInstance(implType, implementation.Id);
        
        // Load configuration if the provider supports it
        if (provider is IProviderWithOptions)
        {
            // Find the options type
            var optionsType = implType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProviderWithOptions<>))
                ?.GetGenericArguments()
                .FirstOrDefault();
            
            if (optionsType != null)
            {
                // Get the options
                var configurationMethod = typeof(IProviderRepository)
                    .GetMethod(nameof(IProviderRepository.GetTypedProviderConfigurationAsync))
                    ?.MakeGenericMethod(optionsType);
                
                if (configurationMethod != null)
                {
                    var options = await (Task<object?>)configurationMethod.Invoke(_repository, new object[] { implementation.Id, cancellationToken })!;
                    
                    if (options != null)
                    {
                        // Register the options in the service provider
                        var registerOptionsMethod = typeof(DynamicServiceProvider)
                            .GetMethod(nameof(DynamicServiceProvider.RegisterOptions))
                            ?.MakeGenericMethod(optionsType);
                        
                        registerOptionsMethod?.Invoke(_serviceProvider, new[] { options });
                    }
                }
            }
        }
        
        // Initialize the provider if it implements IProviderLifecycle
        if (provider is IProviderLifecycle lifecycle)
        {
            await lifecycle.Initialize(cancellationToken);
            await lifecycle.Activate(cancellationToken);
        }
        
        // Register the provider in the service provider
        var registerMethod = typeof(DynamicServiceProvider)
            .GetMethod(nameof(DynamicServiceProvider.RegisterProviderSingleton))
            ?.MakeGenericMethod(providerInterfaceType);
        
        registerMethod?.Invoke(_serviceProvider, new object[] { implType, provider });
        
        return provider;
    }

    /// <summary>
    /// Deactivates a provider.
    /// </summary>
    /// <param name="implementation">The provider implementation.</param>
    /// <param name="providerInterfaceType">The provider interface type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the deactivation operation.</returns>
    private async Task DeactivateProviderAsync(
        ProviderImplementation implementation, 
        Type providerInterfaceType, 
        CancellationToken cancellationToken = default)
    {
        // Load the assembly if necessary
        var assemblyInfo = _assemblyManager.GetAssembly(implementation.AssemblyPath);
        if (assemblyInfo == null)
        {
            try
            {
                assemblyInfo = _assemblyManager.LoadAssembly(implementation.AssemblyPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load assembly for deactivation: {AssemblyPath}", implementation.AssemblyPath);
                return;
            }
        }
        
        try
        {
            // Get the provider from the service provider
            var getServiceMethod = typeof(DynamicServiceProvider)
                .GetMethod(nameof(DynamicServiceProvider.GetService))
                ?.MakeGenericMethod(providerInterfaceType);
            
            if (getServiceMethod != null)
            {
                var provider = getServiceMethod.Invoke(_serviceProvider, Array.Empty<object>()) as IProvider;
                
                if (provider is IProviderLifecycle lifecycle)
                {
                    // Deactivate the provider
                    await lifecycle.Deactivate(cancellationToken);
                }
            }
            
            // Unregister the provider
            var unregisterMethod = typeof(DynamicServiceProvider)
                .GetMethod(nameof(DynamicServiceProvider.UnregisterProvider))
                ?.MakeGenericMethod(providerInterfaceType);
            
            unregisterMethod?.Invoke(_serviceProvider, Array.Empty<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during provider deactivation: {ProviderName}", implementation.FullTypeName);
        }
    }

    /// <summary>
    /// Disposes the provider manager.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        
        _isDisposed = true;
        _lock.Dispose();
    }

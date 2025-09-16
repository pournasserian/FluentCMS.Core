//using FluentCMS.Providers.Abstractions;
//using FluentCMS.Providers.Data;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using System.Collections.Concurrent;

//namespace FluentCMS.Providers.Core;

///// <summary>
///// Factory for resolving and creating provider instances using the main DI container.
///// </summary>
//public class ProviderFactory(IServiceProvider serviceProvider, IProviderManager providerManager, ILogger<ProviderFactory> logger) : IProviderFactory, IDisposable
//{
//    private readonly ConcurrentDictionary<string, IProvider> _providerCache = new();
//    private volatile bool _disposed;

//    /// <summary>
//    /// Gets the active provider for the specified area.
//    /// </summary>
//    public async Task<T?> GetActiveProvider<T>(string area, CancellationToken cancellationToken = default!) where T : class, IProvider
//    {
//        cancellationToken.ThrowIfCancellationRequested();

//        try
//        {
//            var providerEntity = await providerService.GetActiveProvider(area, cancellationToken);
//            if (providerEntity == null)
//            {
//                logger.LogDebug("No active provider found in area {Area}", area);
//                return null;
//            }

//            return await GetProviderInstance<T>(providerEntity, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "Error getting active provider for area {Area}", area);
//            return null;
//        }
//    }

//    /// <summary>
//    /// Gets a specific provider by name within an area.
//    /// </summary>
//    public async Task<T?> GetProvider<T>(string area, string providerName, CancellationToken cancellationToken = default!) where T : class, IProvider
//    {
//        cancellationToken.ThrowIfCancellationRequested();

//        try
//        {
//            var providerEntity = await providerService.GetProvider(area, providerName, cancellationToken);
//            if (providerEntity == null)
//            {
//                logger.LogDebug("Provider {Name} not found in area {Area}", providerName, area);
//                return null;
//            }

//            return await GetProviderInstance<T>(providerEntity, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "Error getting provider {Name} from area {Area}", providerName, area);
//            return null;
//        }
//    }

//    /// <summary>
//    /// Gets all providers in the specified area (both active and inactive).
//    /// </summary>
//    public async Task<IEnumerable<T>> GetProviders<T>(string area, CancellationToken cancellationToken = default!) where T : class, IProvider
//    {
//        cancellationToken.ThrowIfCancellationRequested();

//        try
//        {
//            var providerEntities = await providerService.GetProvidersByArea(area, cancellationToken);
//            var providers = new List<T>();

//            foreach (var entity in providerEntities)
//            {
//                var provider = await GetProviderInstance<T>(entity, cancellationToken);
//                if (provider != null)
//                {
//                    providers.Add(provider);
//                }
//            }

//            return providers;
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "Error getting providers for area {Area}", area);
//            return Array.Empty<T>();
//        }
//    }

//    /// <summary>
//    /// Gets a provider instance using the main DI container and caching.
//    /// </summary>
//    private async Task<T?> GetProviderInstance<T>(Provider providerEntity, CancellationToken cancellationToken = default!) where T : class, IProvider
//    {
//        cancellationToken.ThrowIfCancellationRequested();

//        var cacheKey = $"{providerEntity.Area}:{providerEntity.Name}";

//        // Check cache first
//        if (_providerCache.TryGetValue(cacheKey, out var cachedProvider))
//        {
//            if (cachedProvider is T typedProvider)
//            {
//                logger.LogDebug("Returning cached provider {Name} from area {Area}", providerEntity.Name, providerEntity.Area);
//                return typedProvider;
//            }
//        }

//        try
//        {
//            logger.LogDebug("Creating provider instance for {Name} in area {Area}", providerEntity.Name, providerEntity.Area);

//            // Get the provider module from registry
//            var module = providerRegistry.GetModule(providerEntity.Area, providerEntity.TypeName);
//            if (module == null)
//            {
//                logger.LogWarning("Provider module not found for {Name} in area {Area}", providerEntity.Name, providerEntity.Area);
//                return null;
//            }

//            // Configure provider options if not already configured for this provider instance
//            await EnsureProviderOptionsConfigured(providerEntity, module, cancellationToken);

//            // Try to get the provider from keyed services first
//            var keyedServiceKey = $"{providerEntity.Area}:{providerEntity.Name}";
//            var provider = serviceProvider.GetKeyedService<T>(keyedServiceKey);

//            // Fall back to creating instance with ActivatorUtilities
//            provider ??= ActivatorUtilities.CreateInstance<T>(serviceProvider);

//            if (provider != null)
//            {
//                _providerCache.TryAdd(cacheKey, provider);
//                logger.LogInformation("Successfully created provider instance {Name} in area {Area}", providerEntity.Name, providerEntity.Area);
//                return provider;
//            }

//            logger.LogWarning("Failed to create provider instance {Name} in area {Area}", providerEntity.Name, providerEntity.Area);
//            return null;
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "Failed to create provider instance for {Name} in area {Area}", providerEntity.Name, providerEntity.Area);
//            return null;
//        }
//    }

//    /// <summary>
//    /// Clears the provider cache, forcing recreation of provider instances.
//    /// </summary>
//    public void ClearCache()
//    {
//        logger.LogInformation("Clearing provider cache");

//        // Dispose cached providers if they implement IDisposable
//        foreach (var kvp in _providerCache)
//        {
//            if (kvp.Value is IDisposable disposable)
//            {
//                try
//                {
//                    disposable.Dispose();
//                }
//                catch (Exception ex)
//                {
//                    logger.LogWarning(ex, "Error disposing cached provider {Key}", kvp.Key);
//                }
//            }
//        }

//        _providerCache.Clear();
//    }

//    public void Dispose()
//    {
//        if (!_disposed)
//        {
//            ClearCache();
//            _disposed = true;
//        }
//    }
//}

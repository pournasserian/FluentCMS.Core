# 📝 Enhancement: Comprehensive Logging Implementation

## Issue Description

Implement comprehensive structured logging throughout the provider system to improve observability, debugging, and monitoring capabilities.

## Priority
**High**

## Affected Components
- All provider system components
- `ProviderDiscovery.cs`
- `ProviderManager.cs`
- `ProviderCatalogCache.cs`
- `ProviderRepository.cs`

## Current Logging Gaps

### 1. Missing Logging Infrastructure
- No `ILogger<T>` injection in major components
- `EnableLogging` property exists but is unused
- No structured logging implementation

### 2. Critical Missing Log Points
- Provider discovery operations
- Assembly loading events
- Security validation results
- Cache operations
- Database operations
- Error conditions and exceptions

## Proposed Logging Implementation

### 1. Core Logging Infrastructure
```csharp
// Enhanced ProviderDiscovery with comprehensive logging
internal class ProviderDiscovery(
    ProviderDiscoveryOptions options, 
    ILogger<ProviderDiscovery> logger)
{
    public List<IProviderModule> GetProviderModules()
    {
        using var activity = logger.BeginScope("ProviderDiscovery");
        
        logger.LogInformation("Starting provider discovery. EnableLogging: {EnableLogging}, " +
            "AssemblyPrefixes: {PrefixCount}, IgnoreExceptions: {IgnoreExceptions}",
            options.EnableLogging, options.AssemblyPrefixesToScan.Count, options.IgnoreExceptions);

        var stopwatch = Stopwatch.StartNew();
        var modules = new List<IProviderModule>();
        var errors = new List<string>();

        try
        {
            var dllFiles = GetAssemblyFiles();
            logger.LogInformation("Found {AssemblyCount} assemblies to scan: {AssemblyFiles}", 
                dllFiles.Length, string.Join(", ", dllFiles.Take(5).Select(Path.GetFileName)));

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    logger.LogDebug("Scanning assembly: {AssemblyPath}", dllPath);
                    var assemblyModules = ScanAssembly(dllPath);
                    
                    if (assemblyModules.Any())
                    {
                        logger.LogInformation("Found {ModuleCount} provider modules in {Assembly}: {Modules}",
                            assemblyModules.Count, Path.GetFileName(dllPath),
                            string.Join(", ", assemblyModules.Select(m => $"{m.Name}({m.Area})")));
                        modules.AddRange(assemblyModules);
                    }
                    else
                    {
                        logger.LogDebug("No provider modules found in {Assembly}", Path.GetFileName(dllPath));
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Failed to scan assembly {Path.GetFileName(dllPath)}: {ex.Message}";
                    errors.Add(errorMsg);
                    
                    logger.LogError(ex, "Assembly scan failed for {AssemblyPath}", dllPath);
                    
                    if (!options.IgnoreExceptions)
                        throw;
                }
            }

            stopwatch.Stop();
            logger.LogInformation("Provider discovery completed. Found {ModuleCount} modules in {ElapsedMs}ms. " +
                "Errors: {ErrorCount}",
                modules.Count, stopwatch.ElapsedMilliseconds, errors.Count);

            if (errors.Any())
            {
                logger.LogWarning("Provider discovery completed with {ErrorCount} errors: {Errors}",
                    errors.Count, string.Join("; ", errors));
            }

            return modules;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Provider discovery failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 2. Provider Manager Logging
```csharp
public class ProviderManager(
    IProviderCatalogCache providerCatalogCache,
    ILogger<ProviderManager> logger) : IProviderManager
{
    public async Task<TProvider> GetProvider<TProvider>(string area) where TProvider : class, IProvider
    {
        using var activity = logger.BeginScope("GetProvider");
        logger.LogDebug("Requesting provider for area: {Area}, Type: {ProviderType}", area, typeof(TProvider).Name);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var catalog = await providerCatalogCache.GetCatalogAsync(area);
            
            if (catalog == null)
            {
                logger.LogWarning("No provider catalog found for area: {Area}", area);
                throw new InvalidOperationException($"No provider found for area: {area}");
            }

            logger.LogDebug("Found provider catalog: {ProviderName} for area: {Area}", catalog.Name, area);

            var options = await CreateProviderOptions(catalog);
            var provider = CreateProviderInstance<TProvider>(catalog, options);

            stopwatch.Stop();
            logger.LogInformation("Provider created successfully. Area: {Area}, Provider: {ProviderName}, " +
                "Type: {ProviderType}, ElapsedMs: {ElapsedMs}",
                area, catalog.Name, typeof(TProvider).Name, stopwatch.ElapsedMilliseconds);

            return provider;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Failed to get provider for area: {Area}, Type: {ProviderType}, ElapsedMs: {ElapsedMs}",
                area, typeof(TProvider).Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<object> CreateProviderOptions(ProviderCatalog catalog)
    {
        logger.LogDebug("Creating provider options for: {ProviderName}", catalog.Name);
        
        try
        {
            if (string.IsNullOrWhiteSpace(catalog.Options))
            {
                logger.LogDebug("No options provided for provider: {ProviderName}, using default", catalog.Name);
                return Activator.CreateInstance(catalog.Module.OptionsType)!;
            }

            var options = JsonSerializer.Deserialize(catalog.Options, catalog.Module.OptionsType);
            
            if (options == null)
            {
                logger.LogWarning("JSON deserialization returned null for provider: {ProviderName}, using default", 
                    catalog.Name);
                return Activator.CreateInstance(catalog.Module.OptionsType)!;
            }

            logger.LogDebug("Provider options created successfully for: {ProviderName}", catalog.Name);
            return options;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize options for provider: {ProviderName}. Options: {Options}",
                catalog.Name, catalog.Options);
            throw new InvalidOperationException($"Invalid options for provider {catalog.Name}", ex);
        }
    }
}
```

### 3. Cache Operations Logging
```csharp
public class ProviderCatalogCache(
    IProviderModuleCatalogCache providerModuleCatalogCache,
    ILogger<ProviderCatalogCache> logger) : IProviderCatalogCache
{
    public async Task<ProviderCatalog?> GetCatalogAsync(string area)
    {
        logger.LogDebug("Cache lookup for area: {Area}", area);
        
        if (!IsInitialized)
        {
            logger.LogInformation("Cache not initialized, initializing now for area: {Area}", area);
            await Initialize();
        }

        var exists = _catalogs.TryGetValue(area, out var catalog);
        
        if (exists)
        {
            logger.LogDebug("Cache hit for area: {Area}, Provider: {ProviderName}", area, catalog?.Name);
        }
        else
        {
            logger.LogDebug("Cache miss for area: {Area}", area);
        }

        return catalog;
    }

    private async Task Initialize(CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope("CacheInitialization");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogInformation("Starting provider catalog cache initialization");

            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (_isInitialized)
                {
                    logger.LogDebug("Cache already initialized by another thread");
                    return;
                }

                var catalogs = await providerModuleCatalogCache.GetCatalogsAsync(cancellationToken);
                
                logger.LogInformation("Loaded {CatalogCount} provider catalogs from module cache", catalogs.Count());

                foreach (var catalog in catalogs)
                {
                    _catalogs[catalog.Area] = catalog;
                    logger.LogDebug("Cached provider: {ProviderName} for area: {Area}", catalog.Name, catalog.Area);
                }

                _isInitialized = true;
                stopwatch.Stop();
                
                logger.LogInformation("Provider catalog cache initialization completed. " +
                    "Cached {CatalogCount} catalogs in {ElapsedMs}ms",
                    _catalogs.Count, stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Provider catalog cache initialization failed after {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 4. Repository Operations Logging
```csharp
public class ProviderRepository(
    ProviderDbContext dbContext,
    ILogger<ProviderRepository> logger) : IProviderRepository
{
    public async Task<IEnumerable<Provider>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope("GetAllProviders");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogDebug("Retrieving all providers from database");
            
            var providers = await dbContext.Providers.ToListAsync(cancellationToken);
            
            stopwatch.Stop();
            logger.LogInformation("Retrieved {ProviderCount} providers from database in {ElapsedMs}ms",
                providers.Count, stopwatch.ElapsedMilliseconds);
            
            return providers;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Failed to retrieve providers from database after {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task AddMany(IEnumerable<Provider> providers, CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope("AddManyProviders");
        var providerList = providers.ToList();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogInformation("Adding {ProviderCount} providers to database", providerList.Count);
            
            await dbContext.Providers.AddRangeAsync(providerList, cancellationToken);
            var changes = await dbContext.SaveChangesAsync(cancellationToken);
            
            stopwatch.Stop();
            logger.LogInformation("Successfully added {ProviderCount} providers to database. " +
                "Changes saved: {ChangesCount}, ElapsedMs: {ElapsedMs}",
                providerList.Count, changes, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Failed to add {ProviderCount} providers to database after {ElapsedMs}ms",
                providerList.Count, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## Structured Logging Configuration

### 1. Logging Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "FluentCMS.Providers": "Debug",
      "FluentCMS.Providers.Discovery": "Information",
      "FluentCMS.Providers.Cache": "Information",
      "FluentCMS.Providers.Repository": "Information"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff "
    },
    "ApplicationInsights": {
      "LogLevel": {
        "FluentCMS.Providers": "Information"
      }
    }
  },
  "ProviderLogging": {
    "EnablePerformanceLogging": true,
    "EnableSecurityEventLogging": true,
    "LogSlowOperationsThreshold": 1000,
    "CorrelationIdHeader": "X-Correlation-ID"
  }
}
```

### 2. Structured Logging Extensions
```csharp
public static class LoggerExtensions
{
    public static IDisposable BeginProviderScope(this ILogger logger, string operation, string? area = null)
    {
        var scopeData = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["CorrelationId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString()
        };
        
        if (!string.IsNullOrEmpty(area))
            scopeData["Area"] = area;
            
        return logger.BeginScope(scopeData);
    }
    
    public static void LogProviderOperation(this ILogger logger, LogLevel level, string message, 
        string operation, TimeSpan elapsed, string? area = null, Exception? exception = null)
    {
        using var scope = logger.BeginProviderScope(operation, area);
        
        if (exception != null)
        {
            logger.Log(level, exception, "{Message} - Operation: {Operation}, Elapsed: {ElapsedMs}ms",
                message, operation, elapsed.TotalMilliseconds);
        }
        else
        {
            logger.Log(level, "{Message} - Operation: {Operation}, Elapsed: {ElapsedMs}ms",
                message, operation, elapsed.TotalMilliseconds);
        }
    }
}
```

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1)
- Add ILogger injection to all major components
- Implement basic operation logging
- Add structured logging extensions

### Phase 2: Detailed Event Logging (Week 2)
- Add comprehensive discovery logging
- Implement cache operation logging
- Add database operation logging

### Phase 3: Performance and Security Logging (Week 3)
- Add performance metrics logging
- Implement security event logging
- Add slow operation detection

### Phase 4: Monitoring Integration (Week 4)
- Application Insights integration
- Custom metrics and dashboards
- Alerting configuration

## Monitoring and Alerting

### Key Metrics to Track
- Provider discovery duration
- Cache hit/miss ratios
- Database operation performance
- Error rates by component
- Security validation failures

### Alert Conditions
- Provider discovery taking > 10 seconds
- Cache miss rate > 20%
- Database operations taking > 5 seconds
- Error rate > 5%
- Security validation failures

## Priority
**High** - Critical for production observability

## Labels
- enhancement
- high-priority
- logging
- observability
- monitoring

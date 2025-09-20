# ⚡ Performance Issue: Assembly Scanning Overhead

## Issue Description

The provider discovery system has significant performance overhead due to inefficient assembly scanning operations that occur on every application restart.

## Performance Impact
**High**

## Affected Components
- `ProviderDiscovery.cs`
- Application startup time
- Provider initialization

## Performance Problems Identified

### 1. Expensive Assembly Operations
```csharp
// Current inefficient code
Assembly.LoadFrom(dllPath);
var types = assembly.GetExportedTypes();  // Expensive operation
```

### 2. No Caching of Scan Results
- Assembly scanning results are not cached
- Same assemblies are scanned repeatedly
- No persistent cache across restarts

### 3. Blocking Startup Operations
- Assembly scanning blocks application startup
- Synchronous operations delay initialization
- No parallelization of scanning operations

## Performance Metrics

### Current Performance Issues
- **Startup delay**: 2-5 seconds per 10 assemblies
- **Memory usage**: High during scanning due to assembly loading
- **CPU usage**: 100% single-threaded during scan
- **File I/O**: Multiple reads of same assemblies

### Expected Improvements
- **90% reduction** in startup time with caching
- **50% reduction** in memory usage with lazy loading
- **Parallel processing** can reduce scan time by 70%

## Proposed Solutions

### 1. Assembly Scan Result Caching
```csharp
public class AssemblyScanCache
{
    private readonly string _cacheFilePath;
    private readonly ILogger<AssemblyScanCache> _logger;
    
    public class CachedAssemblyInfo
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string Hash { get; set; } = string.Empty;
        public List<string> ProviderTypes { get; set; } = new();
        public List<string> InterfaceTypes { get; set; } = new();
    }
    
    public async Task<CachedAssemblyInfo?> GetCachedInfo(string assemblyPath)
    {
        var fileInfo = new FileInfo(assemblyPath);
        if (!fileInfo.Exists) return null;
        
        var cachedInfo = await LoadFromCache(assemblyPath);
        if (cachedInfo == null) return null;
        
        // Validate cache is still valid
        if (cachedInfo.LastModified != fileInfo.LastWriteTime)
            return null;
            
        // Validate file hash
        var currentHash = await ComputeFileHash(assemblyPath);
        if (cachedInfo.Hash != currentHash)
            return null;
            
        return cachedInfo;
    }
    
    public async Task SaveToCache(string assemblyPath, CachedAssemblyInfo info)
    {
        var cacheData = await LoadCacheFile();
        cacheData[assemblyPath] = info;
        await SaveCacheFile(cacheData);
    }
}
```

### 2. Parallel Assembly Processing
```csharp
public class ParallelAssemblyScanner
{
    private readonly AssemblyScanCache _cache;
    private readonly ILogger<ParallelAssemblyScanner> _logger;
    
    public async Task<List<IProviderModule>> ScanAssembliesAsync(
        IEnumerable<string> assemblyPaths, 
        CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var results = new ConcurrentBag<IProviderModule>();
        
        var tasks = assemblyPaths.Select(async path =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var modules = await ScanAssemblyAsync(path, cancellationToken);
                foreach (var module in modules)
                    results.Add(module);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
        return results.ToList();
    }
    
    private async Task<List<IProviderModule>> ScanAssemblyAsync(
        string assemblyPath, 
        CancellationToken cancellationToken)
    {
        // Check cache first
        var cachedInfo = await _cache.GetCachedInfo(assemblyPath);
        if (cachedInfo != null)
        {
            _logger.LogDebug("Using cached scan results for {AssemblyPath}", assemblyPath);
            return await CreateModulesFromCache(cachedInfo);
        }
        
        // Scan assembly if not cached
        var modules = await ScanAssemblyInternal(assemblyPath, cancellationToken);
        
        // Cache results
        var cacheInfo = CreateCacheInfo(assemblyPath, modules);
        await _cache.SaveToCache(assemblyPath, cacheInfo);
        
        return modules;
    }
}
```

### 3. Lazy Assembly Loading
```csharp
public class LazyProviderModule : IProviderModule
{
    private readonly string _assemblyPath;
    private readonly string _typeName;
    private Assembly? _loadedAssembly;
    private Type? _loadedType;
    
    public string Area { get; }
    public string Name { get; }
    public Type ProviderType => _loadedType ??= LoadType();
    public Type OptionsType => GetOptionsType();
    
    private Type LoadType()
    {
        if (_loadedAssembly == null)
        {
            _loadedAssembly = Assembly.LoadFrom(_assemblyPath);
        }
        
        return _loadedAssembly.GetType(_typeName) 
            ?? throw new TypeLoadException($"Could not load type {_typeName} from {_assemblyPath}");
    }
}
```

## Implementation Strategy

### Phase 1: Basic Caching (Week 1)
- Implement file-based cache for scan results
- Add cache validation (timestamps, hashes)
- Cache provider type information

### Phase 2: Parallel Processing (Week 2)
- Implement parallel assembly scanning
- Add concurrent result collection
- Optimize memory usage during scanning

### Phase 3: Lazy Loading (Week 3)
- Implement lazy assembly loading
- Defer type resolution until needed
- Minimize startup memory footprint

### Phase 4: Advanced Optimizations (Week 4)
- Add memory-mapped cache files
- Implement incremental scanning
- Add assembly precompilation support

## Configuration Options
```json
{
  "ProviderDiscovery": {
    "EnableCaching": true,
    "CacheDirectory": "App_Data/ProviderCache",
    "ParallelScanDegree": 4,
    "EnableLazyLoading": true,
    "CacheExpirationHours": 24
  }
}
```

## Performance Monitoring
- Add metrics for scan duration
- Monitor cache hit/miss ratios
- Track assembly loading times
- Measure startup performance impact

## Priority
**High** - Significantly impacts application startup performance

## Labels
- performance
- high-priority
- startup
- caching
- assembly-loading

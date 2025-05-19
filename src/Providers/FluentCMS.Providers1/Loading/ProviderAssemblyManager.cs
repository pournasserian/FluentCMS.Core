namespace FluentCMS.Providers.Loading;

/// <summary>
/// Manages loading and unloading of provider assemblies.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProviderAssemblyManager"/> class.
/// </remarks>
/// <param name="logger">The logger.</param>
internal class ProviderAssemblyManager(ILogger<ProviderAssemblyManager> logger) : IDisposable
{
    private readonly ConcurrentDictionary<string, LoadedAssemblyInfo> _loadedAssemblies = new();
    private bool _isDisposed;

    /// <summary>
    /// Loads a provider assembly from the specified path.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly file.</param>
    /// <returns>Information about the loaded assembly.</returns>
    public LoadedAssemblyInfo LoadAssembly(string assemblyPath)
    {
        ThrowIfDisposed();

        // Make sure the file exists
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Provider assembly not found at path: {assemblyPath}", assemblyPath);
        }

        // Get the full path to ensure uniqueness
        var fullPath = Path.GetFullPath(assemblyPath);

        // If the assembly is already loaded, return the existing info
        if (_loadedAssemblies.TryGetValue(fullPath, out var existingInfo))
        {
            return existingInfo;
        }

        try
        {
            // Create a new load context for the assembly
            var loadContext = new ProviderAssemblyLoadContext(fullPath);

            // Load the assembly
            var assembly = loadContext.LoadProviderAssembly();

            // Create and store the loaded assembly info
            var info = new LoadedAssemblyInfo(fullPath, assembly, loadContext);
            _loadedAssemblies[fullPath] = info;

            logger.LogInformation("Loaded provider assembly: {AssemblyName} from {AssemblyPath}", assembly.GetName().Name, fullPath);

            return info;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load provider assembly from {AssemblyPath}", fullPath);
            throw new ProviderLoadException($"Failed to load provider assembly: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Unloads a provider assembly.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly file.</param>
    /// <returns>True if the assembly was unloaded, false if it was not found.</returns>
    public bool UnloadAssembly(string assemblyPath)
    {
        ThrowIfDisposed();

        // Get the full path to ensure uniqueness
        string fullPath = Path.GetFullPath(assemblyPath);

        // If the assembly is not loaded, return false
        if (!_loadedAssemblies.TryRemove(fullPath, out var info))
        {
            return false;
        }

        try
        {
            // Unload the assembly context
            info.LoadContext.Unload();
            logger.LogInformation("Unloaded provider assembly: {AssemblyName} from {AssemblyPath}", info.Assembly.GetName().Name, fullPath);

            // Force garbage collection to help with unloading
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unload provider assembly from {AssemblyPath}", fullPath);
            return false;
        }
    }

    /// <summary>
    /// Gets all loaded provider assemblies.
    /// </summary>
    /// <returns>A collection of loaded assembly information.</returns>
    public IEnumerable<LoadedAssemblyInfo> GetLoadedAssemblies()
    {
        ThrowIfDisposed();

        return [.. _loadedAssemblies.Values];
    }

    /// <summary>
    /// Scans a loaded assembly for provider implementations that implement the specified interface.
    /// </summary>
    /// <typeparam name="TProvider">The provider interface type.</typeparam>
    /// <param name="assemblyInfo">The loaded assembly information.</param>
    /// <returns>A collection of provider implementation types.</returns>
    public IEnumerable<Type> ScanAssemblyForProviders<TProvider>(LoadedAssemblyInfo assemblyInfo) where TProvider : IProvider
    {
        ThrowIfDisposed();

        try
        {
            // Get all types that implement the provider interface
            var assemblies = assemblyInfo.Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(TProvider).IsAssignableFrom(t));

            return [.. assemblies];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to scan assembly {AssemblyName} for providers", assemblyInfo.Assembly.GetName().Name);
            return [];
        }
    }

    /// <summary>
    /// Scans all loaded assemblies for provider implementations that implement the specified interface.
    /// </summary>
    /// <typeparam name="TProvider">The provider interface type.</typeparam>
    /// <returns>A collection of provider implementation types and their assembly info.</returns>
    public IEnumerable<(Type ProviderType, LoadedAssemblyInfo AssemblyInfo)> ScanAssembliesForProviders<TProvider>() where TProvider : IProvider
    {
        ThrowIfDisposed();

        var result = new List<(Type, LoadedAssemblyInfo)>();

        foreach (var assemblyInfo in _loadedAssemblies.Values)
        {
            var providerTypes = ScanAssemblyForProviders<TProvider>(assemblyInfo);
            foreach (var providerType in providerTypes)
            {
                result.Add((providerType, assemblyInfo));
            }
        }

        return result;
    }

    /// <summary>
    /// Scans an assembly for provider interface types.
    /// </summary>
    /// <param name="assemblyInfo">The loaded assembly information.</param>
    /// <returns>A collection of provider interface types.</returns>
    public IEnumerable<Type> ScanAssemblyForProviderInterfaces(LoadedAssemblyInfo assemblyInfo)
    {
        ThrowIfDisposed();

        try
        {
            // Get all types that are interfaces and extend IProvider
            // Exclude the IProvider interface itself 
            var assemblies = assemblyInfo.Assembly.GetTypes()
                .Where(t => t.IsInterface && t != typeof(IProvider) && typeof(IProvider).IsAssignableFrom(t));
            return [.. assemblies];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to scan assembly {AssemblyName} for provider interfaces", assemblyInfo.Assembly.GetName().Name);
            return [];
        }
    }

    /// <summary>
    /// Scans all loaded assemblies for provider interface types.
    /// </summary>
    /// <returns>A collection of provider interface types and their assembly info.</returns>
    public IEnumerable<(Type InterfaceType, LoadedAssemblyInfo AssemblyInfo)> ScanAssembliesForProviderInterfaces()
    {
        ThrowIfDisposed();

        var result = new List<(Type, LoadedAssemblyInfo)>();

        foreach (var assemblyInfo in _loadedAssemblies.Values)
        {
            var interfaceTypes = ScanAssemblyForProviderInterfaces(assemblyInfo);
            foreach (var interfaceType in interfaceTypes)
            {
                result.Add((interfaceType, assemblyInfo));
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a loaded assembly by path.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly file.</param>
    /// <returns>The loaded assembly information, or null if not found.</returns>
    public LoadedAssemblyInfo? GetAssembly(string assemblyPath)
    {
        ThrowIfDisposed();

        // Get the full path to ensure uniqueness
        string fullPath = Path.GetFullPath(assemblyPath);

        _loadedAssemblies.TryGetValue(fullPath, out var info);
        return info;
    }

    /// <summary>
    /// Disposes the assembly manager and unloads all loaded assemblies.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        // Unload all assemblies
        foreach (var assemblyPath in _loadedAssemblies.Keys.ToArray())
        {
            UnloadAssembly(assemblyPath);
        }

        _isDisposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (!_isDisposed) return;
        throw new ObjectDisposedException(nameof(ProviderAssemblyManager));
    }
}

/// <summary>
/// Information about a loaded provider assembly.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LoadedAssemblyInfo"/> class.
/// </remarks>
/// <param name="path">The path to the assembly file.</param>
/// <param name="assembly">The loaded assembly.</param>
/// <param name="loadContext">The assembly load context.</param>
internal class LoadedAssemblyInfo(string path, Assembly assembly, ProviderAssemblyLoadContext loadContext)
{
    /// <summary>
    /// Gets the path to the assembly file.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Gets the loaded assembly.
    /// </summary>
    public Assembly Assembly { get; } = assembly;

    /// <summary>
    /// Gets the assembly load context.
    /// </summary>
    public ProviderAssemblyLoadContext LoadContext { get; } = loadContext;

    /// <summary>
    /// Gets the timestamp when the assembly was loaded.
    /// </summary>
    public DateTimeOffset LoadedAt { get; } = DateTimeOffset.UtcNow;
}

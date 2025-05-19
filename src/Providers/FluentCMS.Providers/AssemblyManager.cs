using System.Collections.Concurrent;

namespace FluentCMS.Providers;

public interface IAssemblyManager : IDisposable
{
    Assembly LoadAssembly(string assemblyPath);
    Task<bool> UnloadAssembly(string assemblyPath, CancellationToken cancellationToken = default);
    bool IsAssemblyLoaded(string assemblyPath);
    IServiceProvider GetServiceProvider(string assemblyPath);
    //T GetService<T>(string assemblyPath) where T : class;
    //bool TryGetService<T>(string assemblyPath, out T service) where T : class;
}

public class AssemblyManager(ILogger logger, IServiceProvider rootServiceProvider) : IAssemblyManager
{
    private readonly ConcurrentDictionary<string, AssemblyContext> _loadedAssemblies = new();
    private bool _isDisposed;

    public Assembly LoadAssembly(string assemblyPath)
    {
        ThrowIfDisposed();

        var normalizedPath = Path.GetFullPath(assemblyPath);

        if (!File.Exists(normalizedPath))
            throw new FileNotFoundException($"Assembly not found: {normalizedPath}");

        // Return existing assembly if already loaded
        if (_loadedAssemblies.TryGetValue(normalizedPath, out var existingContext))
        {
            try
            {
                existingContext.ThrowIfDisposed();
                return existingContext.LoadedAssembly;
            }
            catch (ObjectDisposedException)
            {
                // If the context was disposed but not removed from dictionary
                _loadedAssemblies.TryRemove(normalizedPath, out _);
                // Continue to load a new copy
            }
        }

        var context = new AssemblyContext(normalizedPath, logger);
        var assembly = context.Load();

        if (_loadedAssemblies.TryAdd(normalizedPath, context))
        {
            logger.LogInformation("Assembly loaded: {Path}", normalizedPath);
            return assembly;
        }
        else
        {
            // Another thread loaded the assembly first
            context.Dispose();
            return _loadedAssemblies[normalizedPath].LoadedAssembly;
        }
    }

    public async Task<bool> UnloadAssembly(string assemblyPath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var normalizedPath = Path.GetFullPath(assemblyPath);

        if (_loadedAssemblies.TryGetValue(normalizedPath, out var context))
        {
            // Prepare for unload (wait for pending operations)
            if (!await context.PrepareForUnload(cancellationToken))
            {
                logger.LogWarning("Could not prepare assembly for unload: {Path}", normalizedPath);
                return false;
            }

            // Now actually remove and dispose
            if (_loadedAssemblies.TryRemove(normalizedPath, out _))
            {
                context.Dispose();
                logger.LogInformation("Assembly unloaded: {Path}", normalizedPath);
                return true;
            }
        }

        return false;
    }

    public bool IsAssemblyLoaded(string assemblyPath)
    {
        ThrowIfDisposed();

        var normalizedPath = Path.GetFullPath(assemblyPath);

        if (_loadedAssemblies.TryGetValue(normalizedPath, out var context))
        {
            try
            {
                context.ThrowIfDisposed();
                return true;
            }
            catch (ObjectDisposedException)
            {
                // If the context was disposed but not removed from dictionary
                _loadedAssemblies.TryRemove(normalizedPath, out _);
                return false;
            }
        }

        return false;
    }

    public IServiceProvider GetServiceProvider(string assemblyPath)
    {
        ThrowIfDisposed();

        var normalizedPath = Path.GetFullPath(assemblyPath);

        if (_loadedAssemblies.TryGetValue(normalizedPath, out var context))
        {
            return context.ServiceProvider; // This will throw if the context is disposed
        }

        throw new InvalidOperationException($"Assembly not loaded: {normalizedPath}");
    }

    //public T GetService<T>(string assemblyPath) where T : class
    //{
    //    return GetServiceProvider(assemblyPath).GetService<T>();
    //}

    //public bool TryGetService<T>(string assemblyPath, out T service) where T : class
    //{
    //    service = null;

    //    try
    //    {
    //        service = GetService<T>(assemblyPath);
    //        return service != null;
    //    }
    //    catch (ObjectDisposedException)
    //    {
    //        // The assembly has been unloaded
    //        return false;
    //    }
    //    catch (InvalidOperationException)
    //    {
    //        // The assembly was not loaded
    //        return false;
    //    }
    //}

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        var assemblies = _loadedAssemblies.Values.ToList();
        _loadedAssemblies.Clear();

        foreach (var context in assemblies)
        {
            try
            {
                context.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing assembly context");
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(AssemblyManager));
    }
}
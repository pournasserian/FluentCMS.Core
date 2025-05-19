namespace FluentCMS.Providers;

public class AssemblyContext : IDisposable
{
    private readonly CollectibleAssemblyLoadContext _context;
    private readonly string _assemblyPath;
    private readonly WeakReference? _contextWeakRef;
    private readonly ILogger _logger;
    private ServiceProvider? _serviceProvider;
    private bool _isDisposed;
    private readonly Lock _lock = new();
    private WeakReference? _assemblyWeakRef;

    // Track any active scopes to ensure they get disposed
    private readonly HashSet<IServiceScope> _activeScopes = [];
    private int _unloadLockCount = 0;

    public AssemblyContext(string assemblyPath, ILogger logger)
    {
        _assemblyPath = assemblyPath;
        _logger = logger;
        _context = new CollectibleAssemblyLoadContext(Path.GetFileNameWithoutExtension(assemblyPath));
        _contextWeakRef = new WeakReference(_context);
    }

    public Assembly? LoadedAssembly { get; private set; }

    public IServiceProvider? ServiceProvider
    {
        get
        {
            ThrowIfDisposed();
            return _serviceProvider;
        }
    }

    // Use this to prevent unloading while operations are in progress
    public IDisposable PreventUnload()
    {
        Interlocked.Increment(ref _unloadLockCount);
        return new UnloadLock(this);
    }

    public IServiceScope CreateScope()
    {
        ThrowIfDisposed();
        if (_serviceProvider is null)
            throw new InvalidOperationException("Service provider is not initialized.");

        lock (_lock)
        {
            var scope = _serviceProvider.CreateScope();
            _activeScopes.Add(scope);
            return new TrackedServiceScope(scope, this);
        }
    }

    public Assembly Load()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, nameof(AssemblyContext));

            LoadedAssembly = _context.LoadFromAssemblyPath(_assemblyPath);
            _assemblyWeakRef = new WeakReference(LoadedAssembly);

            return LoadedAssembly;
        }
    }

    public async Task<bool> PrepareForUnload(CancellationToken cancellationToken = default)
    {
        // Wait for any operations in progress to complete
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(30); // Maximum time to wait for operations to complete

        while (Interlocked.CompareExchange(ref _unloadLockCount, 0, 0) > 0)
        {
            if (DateTime.UtcNow - startTime > timeout)
            {
                _logger.LogWarning("Timeout waiting for assembly context operations to complete: {Path}", _assemblyPath);
                return false;
            }

            await Task.Delay(100, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        // Dispose all active scopes
        lock (_lock)
        {
            _logger.LogDebug("Disposing {Count} active scopes for {Path}", _activeScopes.Count, _assemblyPath);

            foreach (var scope in _activeScopes.ToList())
            {
                try
                {
                    scope.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing scope for {Path}", _assemblyPath);
                }
            }

            _activeScopes.Clear();
        }

        return true;
    }

    #region IDisposable

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
        }

        try
        {
            // Let plugins know they're being unloaded through the service provider
            if (_serviceProvider != null)
            {
                try
                {
                    var shutdownHandlers = _serviceProvider.GetServices<IProviderShutdownHandler>();
                    foreach (var handler in shutdownHandlers)
                    {
                        try
                        {
                            handler.OnShutdown();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during plugin shutdown handler execution");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting shutdown handlers");
                }

                // Dispose all active scopes again (safety check)
                lock (_lock)
                {
                    foreach (var scope in _activeScopes.ToList())
                    {
                        try { scope.Dispose(); } catch { }
                    }
                    _activeScopes.Clear();
                }

                // Dispose the service provider to clean up services
                _serviceProvider.Dispose();
                _serviceProvider = null;
            }

            // Make sure all plugin types are no longer used
            // Forces the ALC to unload 
            LoadedAssembly = null;
            _context.Unload();

            // Actually wait for GC to happen and confirm assembly is unloaded
            for (int i = 0; i < 10; i++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();

                if (!_contextWeakRef.IsAlive && (_assemblyWeakRef == null || !_assemblyWeakRef.IsAlive))
                {
                    _logger.LogInformation("Successfully unloaded assembly: {Path}", _assemblyPath);
                    break;
                }

                if (i == 9)
                {
                    _logger.LogWarning("Failed to fully unload assembly after multiple attempts: {Path}. This may indicate a memory leak.", _assemblyPath);
                }

                // Short delay between attempts
                Thread.Sleep(100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during assembly unload: {Path}", _assemblyPath);
        }
    }

    public void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(AssemblyContext),
                $"Cannot access the plugin. The assembly '{_assemblyPath}' has been unloaded.");
    }

    #endregion

    #region Private classes

    private class CollectibleAssemblyLoadContext(string? name) : System.Runtime.Loader.AssemblyLoadContext(name, isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null; // Forces the ALC to only load explicitly given assemblies
        }
    }

    private class TrackedServiceScope(IServiceScope innerScope, AssemblyContext context) : IServiceScope
    {
        private bool _disposed;

        public IServiceProvider ServiceProvider { get => innerScope.ServiceProvider; }

        public void Dispose()
        {
            if (!_disposed)
            {
                innerScope.Dispose();
                lock (context._lock)
                {
                    context._activeScopes.Remove(innerScope);
                }
                _disposed = true;
            }
        }
    }

    private class UnloadLock(AssemblyContext context) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                Interlocked.Decrement(ref context._unloadLockCount);
                _disposed = true;
            }
        }
    }

    #endregion
}
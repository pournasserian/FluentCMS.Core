using Microsoft.Extensions.Configuration;

namespace FluentCMS.Configuration;

/// <summary>
/// Loads configuration from a table where each row = one section, stored as JSON.
/// Table schema: Options (Section TEXT PRIMARY KEY, Value TEXT NOT NULL)
/// </summary>
public sealed class DbConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly DbConfigurationSource _source;
    private readonly Timer? _timer;
    private volatile bool _disposed;
    private volatile bool _databaseEnsured;
    private readonly Lock _disposeLock = new();

    public DbConfigurationProvider(DbConfigurationSource source)
    {
        _source = source;
        if (_source.ReloadInterval is { } interval)
        {
            _timer = new Timer(_ => TriggerReload(), null, interval, interval);
        }
    }

    public override void Load()
    {
        if (_disposed)
            return;

        // Ensure database is created on first load instead of in constructor
        // This avoids blocking async calls in constructor which can cause deadlocks
        if (!_databaseEnsured)
        {
            _source.Repository.EnsureCreated().GetAwaiter().GetResult();
            _databaseEnsured = true;
        }

        Data = _source.Repository.GetAllSections().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Call after you change DB programmatically to force consumers to refresh.
    /// </summary>
    public void TriggerReload()
    {
        if (_disposed)
            return;

        Load();
        OnReload();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_disposeLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    // Finalizer to ensure timer is disposed even if Dispose is not called
    ~DbConfigurationProvider()
    {
        Dispose();
    }
}

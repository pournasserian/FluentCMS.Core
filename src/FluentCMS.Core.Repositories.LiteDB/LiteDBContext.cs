namespace FluentCMS.Core.Repositories.LiteDB;

public class LiteDBContext : ILiteDBContext, IDisposable
{
    private readonly LiteDatabase _database;
    private bool _disposed = false;

    public LiteDBContext(LiteDBOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(options.ConnectionString, nameof(options.ConnectionString));

        // Configure connection parameters
        var connectionString = options.ConnectionString;

        // Configure mapper if provided
        var mapper = new BsonMapper();
        options.MapperConfiguration?.Invoke(mapper);
        _database = new LiteDatabase(connectionString, mapper);
    }

    public ILiteDatabase Database => _database;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _database?.Dispose();
            }

            _disposed = true;
        }
    }
}
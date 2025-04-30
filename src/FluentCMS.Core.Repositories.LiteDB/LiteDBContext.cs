namespace FluentCMS.Core.Repositories.LiteDB;

public interface ILiteDBContext
{
    ILiteDatabase Database { get; }
}

public class LiteDBContext : ILiteDBContext, IDisposable
{
    private bool _disposed = false;

    public LiteDBContext(IOptions<LiteDBOptions> options)
    {
        // Configure connection parameters
        var connectionString = options.Value.ConnectionString;

        // Configure mapper if provided
        var mapper = new BsonMapper();
        //options.MapperConfiguration?.Invoke(mapper);
        Database = new LiteDatabase(connectionString, mapper);
    }

    public ILiteDatabase Database { get; private set; }

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
                Database?.Dispose();
            }

            _disposed = true;
        }
    }
}
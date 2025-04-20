using LiteDB;

namespace FluentCMS.Core.Repositories.LiteDB;

public class LiteDBContext : ILiteDBContext, IDisposable
{
    private readonly LiteDatabase _database;
    private bool _disposed = false;

    public LiteDBContext(string connectionString)
    {
        _database = new LiteDatabase(connectionString);
        
        //// Map Guid to BSON
        //BsonMapper.Global.RegisterType(
        //    serialize: (guid) => guid.ToString(),
        //    deserialize: (bson) => Guid.Parse(bson.AsString)
        //);
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
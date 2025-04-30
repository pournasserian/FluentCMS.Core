namespace FluentCMS.Core.Repositories.MongoDB;

public interface IMongoDBContext
{
    IMongoDatabase Database { get; }
}

public class MongoDBContext : IMongoDBContext, IDisposable
{
    private bool _disposed = false;

    public IMongoDatabase Database { get; private set; }
    public IMongoClient Client { get; private set; }

    public MongoDBContext(IOptions<MongoDBOptions> options)
    {
        // Create a MongoUrl object based on the provided connection string.
        var mongoUrl = new MongoUrl(options.Value.ConnectionString);

        // Create a MongoClient using the MongoUrl.
        Client = new MongoClient(mongoUrl);

        // Initialize the Database property using the database name from the MongoUrl.
        Database = Client.GetDatabase(mongoUrl.DatabaseName);
    }

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
                Client?.Dispose();
            }

            _disposed = true;
        }
    }
}

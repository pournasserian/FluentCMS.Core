using FluentCMS.Core.Repositories.LiteDB;

namespace FluentCMS.Core.Repositories.Tests;

// Test fixture that creates a new in-memory LiteDB database
public class LiteDBContextFixture : IDisposable
{
    private bool _disposed;
    
    public LiteDBContext Context { get; }
    
    public LiteDBContextFixture()
    {
        // Create a unique in-memory database for each test
        var dbName = $"mem:{Guid.NewGuid()}";
        var options = new LiteDBOptions
        {
            ConnectionString = dbName
        };
        
        Context = new LiteDBContext(options);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
            
        if (disposing)
        {
            // Dispose context to close the database connection
            Context.Dispose();
        }
        
        _disposed = true;
    }
}

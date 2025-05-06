using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace FluentCMS.DataAccess.EntityFramework;


public class UnitOfWork<TContext>(TContext context, IServiceProvider serviceProvider) : IUnitOfWork where TContext : DbContext
{
    private readonly ConcurrentDictionary<Type, object> _repositories = [];
    private readonly RepositoryRegistry _repositoryRegistry = serviceProvider.GetRequiredService<RepositoryRegistry>();

    protected TContext Context => context;

    public virtual IRepository<T> Repository<T>() where T : class, IEntity
    {
        var entityType = typeof(T);

        if (_repositories.TryGetValue(entityType, out object? value))
            return (IRepository<T>)value;


        // Get custom repository interface type if it exists
        var customRepositoryInterfaceType = _repositoryRegistry.GetRepositoryInterfaceType(entityType);

        if (customRepositoryInterfaceType != null)
        {
            // Try to resolve from DI container first
            var customRepositoryInstance = serviceProvider.GetService(customRepositoryInterfaceType);

            if (customRepositoryInstance != null)
            {
                // Cache the custom repository instance
                _repositories.TryAdd(entityType, customRepositoryInstance);
                return (IRepository<T>)customRepositoryInstance;
            }
        }

        // Fallback to the default repository implementation
        var genericRepositoryInstance = serviceProvider.GetRequiredService<IRepository<T>>();
        _repositories.TryAdd(entityType, genericRepositoryInstance);
        return genericRepositoryInstance;
    }

    public virtual async Task SaveChanges(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Handle specific exceptions if needed
            throw new RepositoryOperationException("An error occurred while saving changes to the database.", ex);
        }
    }

    #region IDisposable Members

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                context.Dispose();

                foreach (IRepository repository in _repositories.Values)
                    repository.Dispose();

                _repositories.Clear();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Members
}


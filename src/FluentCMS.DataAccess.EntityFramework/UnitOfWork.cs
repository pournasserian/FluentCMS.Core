using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace FluentCMS.DataAccess.EntityFramework;

public class UnitOfWork<TContext>(TContext context, IServiceProvider serviceProvider) : IUnitOfWork where TContext : DbContext
{
    private readonly ConcurrentDictionary<string, IRepository> _repositories = [];

    protected TContext Context => context;

    public virtual T Repository<T>() where T : IRepository
    {
        T repositoryInstance;

        var key = typeof(T).FullName ??
            throw new ArgumentNullException(nameof(T));

        if (_repositories.TryGetValue(key, out var repository))
            repositoryInstance = (T)repository;
        else
        {
            repositoryInstance = ActivatorUtilities.CreateInstance<T>(serviceProvider, context);
            _repositories.TryAdd(key, repositoryInstance);
        }
        return repositoryInstance;
    }

    public virtual async Task SaveChanges(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
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

                foreach (var repository in _repositories.Values)
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


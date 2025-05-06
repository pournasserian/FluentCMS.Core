using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FluentCMS.DataAccess.EntityFramework;

public class Repository<T>(DbContext context) : IRepository<T> where T : class, IEntity
{
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public virtual async Task<T> Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await DbSet.FindAsync([id], cancellationToken: cancellationToken).ConfigureAwait(false) ??
            throw new EntityNotFoundException(id.ToString()!, typeof(T).Name);

        DbSet.Remove(entity);

        return entity;
    }

    public virtual async Task<T> Add(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        await context.AddAsync(entity, cancellationToken).ConfigureAwait(false);

        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddMany(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await context.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
        return entities;
    }

    public virtual Task<T> Remove(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        DbSet.Remove(entity);

        return Task.FromResult(entity);
    }

    public virtual Task<T> Update(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        DbSet.Update(entity);

        return Task.FromResult(entity);
    }

    public virtual async Task<T?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbSet.SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<bool> Any(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (filter == null)
        {
            return await DbSet.AnyAsync(cancellationToken).ConfigureAwait(false);
        }
        return await DbSet.AnyAsync(filter, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<long> Count(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (filter == null)
        {
            return await DbSet.CountAsync(cancellationToken).ConfigureAwait(false);
        }
        return await DbSet.CountAsync(filter, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IEnumerable<T>> Find(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbSet.Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IEnumerable<T>> GetAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbSet.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    #region IDisposable Members

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
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
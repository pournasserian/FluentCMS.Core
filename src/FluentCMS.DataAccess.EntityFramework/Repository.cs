using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FluentCMS.DataAccess.EntityFramework;

public class Repository<T>(DbContext context) : IRepository<T> where T : class, IEntity
{
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public virtual async Task<T?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbSet.SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<T> Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await GetById(id, cancellationToken).ConfigureAwait(false) ??
            throw new EntityNotFoundException(id.ToString()!, typeof(T).Name);

        return DbSet.Remove(entity).Entity;
    }

    public virtual async Task<T> Add(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        await context.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddMany(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entity in entities)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entities), "Entities collection contains null values.");
            }
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }
        }

        await context.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
        return entities;
    }

    public virtual async Task<T> Remove(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        return await Task.FromResult(DbSet.Remove(entity).Entity).ConfigureAwait(false);
    }

    public virtual Task<T> Update(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        return Task.FromResult(DbSet.Update(entity).Entity);
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
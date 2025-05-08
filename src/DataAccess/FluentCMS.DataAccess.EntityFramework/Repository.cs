using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FluentCMS.DataAccess.EntityFramework;

public class Repository<TEntity, TContext> : 
    IRepository<TEntity> 
    where TEntity : class, IEntity 
    where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly DbSet<TEntity> DbSet;
    protected virtual QueryTrackingBehavior QueryTrackingBehavior { get => QueryTrackingBehavior.NoTracking; }
    protected virtual bool AutoDetectChangesEnabled { get => false; }

    public Repository(TContext context)
    {
        Context = context ??
            throw new ArgumentNullException(nameof(context));

        DbSet = context.Set<TEntity>();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior;
        context.ChangeTracker.AutoDetectChangesEnabled = AutoDetectChangesEnabled;
    }

    public virtual async Task<TEntity> Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await DbSet.FindAsync([id], cancellationToken) ??
            throw new EntityNotFoundException(id.ToString()!, typeof(TEntity).Name);

        DbSet.Remove(entity);
        Context.Entry(entity).State = EntityState.Deleted;

        var affectedRows = await Context.SaveChangesAsync(cancellationToken);

        if (affectedRows != 1)
        {
            // TODO: Log this or throw an exception
        }

        return entity;
    }

    public virtual async Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            await Context.AddAsync(entity, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log this or throw an exception
            throw;
        }

        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> AddMany(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await Context.AddRangeAsync(entities, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log this or throw an exception
            throw;
        }

        return entities;
    }

    public virtual async Task<TEntity> Remove(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        try
        {

            DbSet.Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log this or throw an exception
            throw;
        }

        return entity;
    }

    public virtual async Task<TEntity> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            DbSet.Update(entity);
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log this or throw an exception
            throw;
        }

        return entity;
    }

    public virtual async Task<TEntity?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await DbSet.SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log this or throw an exception
            throw;
        }
    }

    public virtual async Task<bool> Any(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (filter == null)
            {
                return await DbSet.AnyAsync(cancellationToken);
            }
            return await DbSet.AnyAsync(filter, cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log this or throw an exception
            throw;
        }
    }

    public virtual async Task<long> Count(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (filter == null)
            {
                return await DbSet.CountAsync(cancellationToken);
            }
            return await DbSet.CountAsync(filter, cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log this or throw an exception
            throw;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> Find(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(predicate);

        try
        {
            return await DbSet.Where(predicate).ToListAsync(cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log this or throw an exception
            throw;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> GetAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await DbSet.ToListAsync(cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log this or throw an exception
            throw;
        }
    }
}
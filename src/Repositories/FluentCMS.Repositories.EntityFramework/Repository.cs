﻿namespace FluentCMS.Repositories.EntityFramework;

public class Repository<TEntity, TContext>(TContext context, ILogger<Repository<TEntity, TContext>> logger) : IRepository<TEntity>
    where TEntity : class, IEntity
    where TContext : DbContext
{
    protected readonly TContext Context = context ??
            throw new ArgumentNullException(nameof(context));

    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public virtual async Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            await Context.AddAsync(entity, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Entity {EntityType} with id {EntityId} added", typeof(TEntity).Name, entity.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to Add entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
            throw new RepositoryException<TEntity>($"Unable to Add entity {typeof(TEntity).Name} with id {entity.Id}", ex);
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

            logger.LogInformation("Entities {EntityType} added", typeof(TEntity).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to AddMany entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException<TEntity>($"Unable to AddMany entity {typeof(TEntity).Name}", ex);
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
            var affectedRows = await Context.SaveChangesAsync(cancellationToken);
            if (affectedRows == 0)
            {
                logger.LogError("Unable to Remove entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
                throw new RepositoryException<TEntity>($"Unable to Remove entity {typeof(TEntity).Name} with id {entity.Id}");
            }
            else if (affectedRows > 1)
            {
                logger.LogError("More than one entity was removed for entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
                throw new RepositoryException<TEntity>($"More than one entity was removed for Entity {typeof(TEntity).Name} with id {entity.Id}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to Remove entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
            throw new RepositoryException<TEntity>($"Unable to Remove entity {typeof(TEntity).Name} with id {entity.Id}", ex);
        }

        logger.LogInformation("Entity {EntityType} with id {EntityId} removed", typeof(TEntity).Name, entity.Id);

        return entity;
    }

    public virtual async Task<TEntity> Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await DbSet.FindAsync([id], cancellationToken) ??
            throw new RepositoryException<TEntity>($"Entity {typeof(TEntity).Name} with id {id} not found.");

        return await Remove(entity, cancellationToken);
    }

    public virtual async Task<TEntity> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            DbSet.Update(entity);
            await Context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Entity {EntityType} with id {EntityId} updated", typeof(TEntity).Name, entity.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to Update entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
            throw new RepositoryException<TEntity>($"Unable to Update entity {typeof(TEntity).Name} with id {entity.Id}", ex);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable in GetById for entity {EntityType} with id {EntityId}", typeof(TEntity).Name, id);
            throw new RepositoryException<TEntity>($"Unable in GetById for entity {typeof(TEntity).Name} with id {id}", ex);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable in Any for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException<TEntity>($"Unable in Any for entity {typeof(TEntity).Name}", ex);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable in Count for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException<TEntity>($"Unable in Count for entity {typeof(TEntity).Name}", ex);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable in Find for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException<TEntity>($"Unable in Find for entity {typeof(TEntity).Name}", ex);
        }
    }

    public virtual async Task<IEnumerable<TEntity>> GetAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await DbSet.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable in GetAll for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException<TEntity>($"Unable in GetAll for entity {typeof(TEntity).Name}", ex);
        }
    }
}
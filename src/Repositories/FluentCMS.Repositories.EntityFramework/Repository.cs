namespace FluentCMS.Repositories.EntityFramework;

public class Repository<TEntity, TContext> : IRepository<TEntity>
    where TEntity : class, IEntity
    where TContext : DbContext
{
    protected readonly ILogger Logger = default!;
    protected readonly TContext Context = default!;
    protected readonly DbSet<TEntity> DbSet = default!;

    public Repository(TContext context)
    {
        Logger = StaticLoggerFactory.CreateLogger(GetType());
        Context = context ??
            throw new ArgumentNullException(nameof(context));
        DbSet = Context.Set<TEntity>();
    }

    public virtual async Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        try
        {
            await Context.AddAsync(entity, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Entity {EntityType} with id {EntityId} added", typeof(TEntity).Name, entity.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to Add entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
            throw new RepositoryException<TEntity>($"Unable to Add entity {typeof(TEntity).Name} with id {entity.Id}", ex);
        }

        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> AddMany(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entity in entities)
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();
        }

        try
        {
            await Context.AddRangeAsync(entities, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Entities {EntityType} added", typeof(TEntity).Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to AddMany entity {EntityType}", typeof(TEntity).Name);
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
                Logger.LogError("Unable to Remove entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
                throw new RepositoryException<TEntity>($"Unable to Remove entity {typeof(TEntity).Name} with id {entity.Id}");
            }
            else if (affectedRows > 1)
            {
                Logger.LogError("More than one entity was removed for entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
                throw new RepositoryException<TEntity>($"More than one entity was removed for Entity {typeof(TEntity).Name} with id {entity.Id}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to Remove entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
            throw new RepositoryException<TEntity>($"Unable to Remove entity {typeof(TEntity).Name} with id {entity.Id}", ex);
        }

        Logger.LogInformation("Entity {EntityType} with id {EntityId} removed", typeof(TEntity).Name, entity.Id);

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

            Logger.LogInformation("Entity {EntityType} with id {EntityId} updated", typeof(TEntity).Name, entity.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to Update entity {EntityType} with id {EntityId}", typeof(TEntity).Name, entity.Id);
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
            Logger.LogError(ex, "Unable in GetById for entity {EntityType} with id {EntityId}", typeof(TEntity).Name, id);
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
            Logger.LogError(ex, "Unable in Any for entity {EntityType}", typeof(TEntity).Name);
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
            Logger.LogError(ex, "Unable in Count for entity {EntityType}", typeof(TEntity).Name);
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
            Logger.LogError(ex, "Unable in Find for entity {EntityType}", typeof(TEntity).Name);
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
            Logger.LogError(ex, "Unable in GetAll for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException<TEntity>($"Unable in GetAll for entity {typeof(TEntity).Name}", ex);
        }
    }
}
namespace FluentCMS.Repositories.EntityFramework;

public class Repository<TEntity, TContext>(TContext context) : IRepository<TEntity> where TEntity : class, IEntity where TContext : DbContext
{
    protected readonly TContext Context = context ??
            throw new ArgumentNullException(nameof(context));

    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public virtual async Task<TEntity> Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var entity = await DbSet.FindAsync([id], cancellationToken) ??
            throw new RepositoryException<TEntity>($"Entity {typeof(TEntity).Name} with id {id} not found.");

        return await Remove(entity, cancellationToken);
    }

    public virtual async Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            await Context.AddAsync(entity, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new RepositoryException<TEntity>($"Unable to Add Entity {typeof(TEntity).Name} with id {entity.Id}", ex);
        }

        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> AddMany(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            foreach (var entity in entities)
            {
                if (entity.Id == Guid.Empty)
                    entity.Id = Guid.NewGuid();
            }
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
            var affectedRows = await Context.SaveChangesAsync(cancellationToken);
            if (affectedRows == 0)
            {
                throw new RepositoryException<TEntity>($"Unable to Remove Entity {typeof(TEntity).Name} with id {entity.Id}");
            }
            else if (affectedRows > 1)
            {
                throw new RepositoryException<TEntity>($"More than one entity was removed for Entity {typeof(TEntity).Name} with id {entity.Id}");
            }
        }
        catch (Exception ex)
        {
            throw new RepositoryException<TEntity>($"Unable to Remove Entity {typeof(TEntity).Name} with id {entity.Id}", ex);
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
        catch (Exception ex)
        {
            throw new RepositoryException<TEntity>($"Unable to Update Entity {typeof(TEntity).Name} with id {entity.Id}", ex);
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
            throw new RepositoryException<TEntity>($"Unable in GetById for Entity {typeof(TEntity).Name} with id {id}", ex);
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
            throw new RepositoryException<TEntity>($"Unable in Any for Entity {typeof(TEntity).Name}", ex);
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
        catch (Exception ex )
        {
            throw new RepositoryException<TEntity>($"Unable in Count for Entity {typeof(TEntity).Name}", ex);
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
        catch (Exception ex )
        {
            throw new RepositoryException<TEntity>($"Unable in Find for Entity {typeof(TEntity).Name}", ex);
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
            throw new RepositoryException<TEntity>($"Unable in GetAll for Entity {typeof(TEntity).Name}", ex);
        }
    }
}
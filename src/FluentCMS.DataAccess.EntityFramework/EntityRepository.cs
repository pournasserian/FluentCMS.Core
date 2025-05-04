using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataAccess.EntityFramework;

public class EntityRepository<TEntity>(DbContext context) :
    Repository<TEntity>(context), 
    IEntityRepository<TEntity>
    where TEntity : class, IEntity
{
    public virtual async Task<TEntity?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbSet.SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<TEntity> Remove(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await GetById(id, cancellationToken).ConfigureAwait(false) ??
            throw new EntityNotFoundException(id.ToString()!, typeof(TEntity).Name);

        return DbSet.Remove(entity).Entity;
    }

    public override Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        return base.Add(entity, cancellationToken);
    }

    public override Task<IEnumerable<TEntity>> AddMany(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entities);
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

        return base.AddMany(entities, cancellationToken);
    }
}

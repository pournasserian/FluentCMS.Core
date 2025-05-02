using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataAccess.EntityFramework;

public class EntityRepository<TEntity, TKey>(DbContext context) :
    Repository<TEntity>(context), IEntityRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    public virtual async Task<TEntity?> GetById(TKey id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(id);

        return await DbSet.SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<TEntity> Remove(TKey id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(id);

        var entity = await GetById(id, cancellationToken).ConfigureAwait(false) ??
            throw new EntityNotFoundException(id.ToString()!, typeof(TEntity).Name);

        return DbSet.Remove(entity).Entity;
    }
}

public class EntityRepository<TEntity>(DbContext context) : EntityRepository<TEntity, Guid>(context), IEntityRepository<TEntity>
    where TEntity : class, IEntity
{
}
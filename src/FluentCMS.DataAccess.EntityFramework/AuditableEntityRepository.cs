using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataAccess.EntityFramework;

public class AuditableEntityRepository<TEntity, TKey>(DbContext context, IApplicationExecutionContext applicationExecutionContext) :
    EntityRepository<TEntity, TKey>(context), IAuditableEntityRepository<TEntity, TKey>
    where TEntity : class, IAuditableEntity<TKey>
    where TKey : IEquatable<TKey>
{
    public override Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        entity.CreatedBy = applicationExecutionContext.Username;
        entity.CreatedAt = DateTime.UtcNow;

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
            entity.CreatedBy = applicationExecutionContext.Username;
            entity.CreatedAt = DateTime.UtcNow;
            entity.Version = 1;
        }

        return base.AddMany(entities, cancellationToken);
    }

    public override Task<TEntity> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);

        entity.UpdatedBy = applicationExecutionContext.Username;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Version++;

        return base.Update(entity, cancellationToken);
    }

}

public class AuditableEntityRepository<TEntity>(DbContext context, IApplicationExecutionContext applicationExecutionContext) :
    AuditableEntityRepository<TEntity, Guid>(context, applicationExecutionContext), IAuditableEntityRepository<TEntity>
    where TEntity : class, IAuditableEntity
{
}
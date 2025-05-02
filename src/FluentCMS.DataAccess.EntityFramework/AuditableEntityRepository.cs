using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.DataAccess.EntityFramework;

public class AuditableEntityRepository<TEntity, TKey>(DbContext context) :
    EntityRepository<TEntity, TKey>(context), IAuditableEntityRepository<TEntity, TKey>
    where TEntity : class, IAuditableEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

public class AuditableEntityRepository<TEntity>(DbContext context) :
    AuditableEntityRepository<TEntity, Guid>(context), IAuditableEntityRepository<TEntity>
    where TEntity : class, IAuditableEntity
{
}
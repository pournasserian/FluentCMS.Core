namespace FluentCMS.DataAccess.Abstractions;

public interface IAuditableEntityRepository<TEntity, TKey> : IEntityRepository<TEntity, TKey> where TEntity : class, IAuditableEntity<TKey> where TKey : IEquatable<TKey>
{
}

public interface IAuditableEntityRepository<TEntity> : IAuditableEntityRepository<TEntity, Guid> where TEntity : class, IAuditableEntity
{
}

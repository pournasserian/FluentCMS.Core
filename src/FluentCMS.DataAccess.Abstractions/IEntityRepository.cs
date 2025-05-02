namespace FluentCMS.DataAccess.Abstractions;

public interface IEntityRepository<TEntity, TKey> : IRepository<TEntity> where TEntity : class, IEntity<TKey> where TKey : IEquatable<TKey>
{
    Task<TEntity?> GetById(TKey id, CancellationToken cancellationToken = default);
    Task<TEntity> Remove(TKey id, CancellationToken cancellationToken = default);
}

public interface IEntityRepository<TEntity> : IEntityRepository<TEntity, Guid> where TEntity : class, IEntity
{

}
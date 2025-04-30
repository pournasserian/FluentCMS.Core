namespace FluentCMS.Core.Repositories.Abstractions;

public interface IEntityRepository<TEntity> : IRepository where TEntity : class, IEntity
{
    IQueryable<TEntity> AsQueryable();
    Task<IEnumerable<TEntity>> GetAll(CancellationToken cancellationToken = default);
    Task<QueryResult<TEntity>> Query(QueryOptions<TEntity> options, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> Find(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<long> Count(Expression<Func<TEntity, bool>>? filter = default, CancellationToken cancellationToken = default);
    Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> Update(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> Remove(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity> Remove(Guid id, CancellationToken cancellationToken = default);
}

namespace FluentCMS.Core.Repositories.Abstractions;

public interface IEntityRepository<T> where T : class, IEntity
{
    Task<IEnumerable<T>> GetAll(CancellationToken cancellationToken = default);
    Task<QueryResult<T>> Query(QueryOptions<T> options, CancellationToken cancellationToken = default);
    Task<long> Count(Expression<Func<T, bool>>? filter = default, CancellationToken cancellationToken = default);
    Task<T> Add(T entity, CancellationToken cancellationToken = default);
    Task<T> Update(T entity, CancellationToken cancellationToken = default);
    Task<T> Remove(T entity, CancellationToken cancellationToken = default);
    Task<T> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<T> Remove(Guid id, CancellationToken cancellationToken = default);
}

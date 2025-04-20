using System.Linq.Expressions;

namespace FluentCMS.Core.Repositories.Abstractions;

public interface IBaseEntityRepository<T> where T : IBaseEntity
{
    Task<T?> GetById(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> GetAll(CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> Query(Expression<Func<T, bool>>? filter = default, SortOptions<T>? sortOptions = default, PaginationOptions? paginationOptions = default, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> Query(QueryOptions<T> options, CancellationToken cancellationToken = default);

    Task<int> Count(Expression<Func<T, bool>>? filter = default, CancellationToken cancellationToken = default);

    Task Add(T entity, CancellationToken cancellationToken = default);

    Task AddMany(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    Task Update(T entity, CancellationToken cancellationToken = default);

    Task Delete(Guid id, CancellationToken cancellationToken = default);

    Task DeleteMany(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
}

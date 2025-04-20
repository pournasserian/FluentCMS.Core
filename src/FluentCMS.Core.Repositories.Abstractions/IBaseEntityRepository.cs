using System.Linq.Expressions;

namespace FluentCMS.Core.Repositories.Abstractions;

public interface IBaseEntityRepository<T> where T : IBaseEntity
{
    Task<T?> GetById(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> GetAll(CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> Query(Expression<Func<T, bool>>? filter = default, PaginationOptions? paginationOptions = default, IList<SortOption<T>>? sortOptions = default, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> Query(QueryOptions<T> options, CancellationToken cancellationToken = default);

    Task<int> Count(Expression<Func<T, bool>>? filter = default, CancellationToken cancellationToken = default);

    Task<T> Add(T entity, CancellationToken cancellationToken = default);

    Task<T> Update(T entity, CancellationToken cancellationToken = default);

    Task Remove(Guid id, CancellationToken cancellationToken = default);
}

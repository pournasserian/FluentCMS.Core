using System.Linq.Expressions;

namespace FluentCMS.DataAccess.Abstractions;

public interface IRepository : IDisposable
{
}

public interface IRepository<T> : IRepository where T : class
{
    Task<T> Add(T entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> AddMany(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task<T> Update(T entity, CancellationToken cancellationToken = default);
    Task<T> Remove(T entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAll(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> Find(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<long> Count(Expression<Func<T, bool>>? filter = default, CancellationToken cancellationToken = default);
    Task<bool> Any(Expression<Func<T, bool>>? filter = default, CancellationToken cancellationToken = default);
}

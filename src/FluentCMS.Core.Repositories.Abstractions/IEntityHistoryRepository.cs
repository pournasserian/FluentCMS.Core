namespace FluentCMS.Core.Repositories.Abstractions;

public interface IEntityHistoryRepository<T> where T : IEntity
{
    Task<IEnumerable<EntityHistory<T>>> GetAll(Guid entityId, CancellationToken cancellationToken = default);
    Task<EntityHistory<T>> Add(T entity, string action, CancellationToken cancellationToken = default);
    Task<IEnumerable<EntityHistory<T>>> GetHistoryByDateRange(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<EntityHistory<T>?> GetLatestHistoryForEntity(Guid entityId, CancellationToken cancellationToken = default);
}

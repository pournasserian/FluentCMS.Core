namespace FluentCMS.Core.Plugins.History;

public interface IEntityHistoryRepository
{
    Task<EntityHistory> Add<T>(T entity, string actionName, CancellationToken cancellationToken = default) where T : class, IAuditableEntity;
    Task<QueryResult<EntityHistory>> GetByEntityId(Guid entityId, CancellationToken cancellationToken = default);
}

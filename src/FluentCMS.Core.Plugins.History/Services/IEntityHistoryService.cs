namespace FluentCMS.Core.Plugins.History.Services;

public interface IEntityHistoryService
{
    Task<EntityHistory> Add<T>(object entity, string eventType, CancellationToken cancellationToken = default) where T : class, IAuditableEntity;
    Task<QueryResult<EntityHistory>> GetByEntityId(Guid entityId, CancellationToken cancellationToken = default);
}

namespace FluentCMS.Core.Plugins.History.Services;

public class EntityHistoryService(IEntityHistoryRepository entityHistoryRepository) : IEntityHistoryService
{
    public async Task<EntityHistory> Add<T>(object entity, string eventType, CancellationToken cancellationToken = default) where T : class, IAuditableEntity
    {
        return await entityHistoryRepository.Add((T)entity, eventType, cancellationToken);
    }

    public async Task<QueryResult<EntityHistory>> GetByEntityId(Guid entityId, CancellationToken cancellationToken = default)
    {
        return await entityHistoryRepository.GetByEntityId(entityId, cancellationToken);
    }
}

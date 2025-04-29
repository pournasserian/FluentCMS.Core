namespace FluentCMS.Core.Plugins.History.Repositories;

public class EntityHistoryRepository(IEntityRepository<EntityHistory> entityRepository, ApiExecutionContext executionContext): IEntityHistoryRepository
{
    public async Task<EntityHistory> Add<T>(T entity, string eventType, CancellationToken cancellationToken = default) where T : class, IAuditableEntity
    {
        // check if the type is EntityHostory do not add history
        if (entity is EntityHistory selfHistory)
            return selfHistory;

        var entityHistory = new EntityHistory
        {
            Id = Guid.NewGuid(),
            Context = executionContext,
            EntityId = entity.Id,
            EntityType = typeof(T).Name,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Entity = entity
        };
        return await entityRepository.Add(entityHistory, cancellationToken);
    }

    public async Task<QueryResult<EntityHistory>> GetByEntityId(Guid entityId, CancellationToken cancellationToken = default)
    {
        var queryOptions = new QueryOptions<EntityHistory>()
        {
            Filter = (x) => x.EntityId == entityId 
        };
        return await entityRepository.Query(queryOptions, cancellationToken);
    }
}
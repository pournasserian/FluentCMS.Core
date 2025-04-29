namespace FluentCMS.Core.Plugins.History.Repositories;

public class EntityHistoryRepository(IEntityRepository<EntityHistory> entityRepository, IApplicationExecutionContext executionContext) : IEntityHistoryRepository
{
    public async Task<EntityHistory> Add<T>(T entity, string eventType, CancellationToken cancellationToken = default) where T : class, IAuditableEntity
    {
        // check if the type is EntityHostory do not add history
        if (entity is EntityHistory selfHistory)
            return selfHistory;

        var entityHistory = new EntityHistory
        {
            Id = Guid.NewGuid(),
            EntityId = entity.Id,
            EntityType = typeof(T).Name,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Entity = entity,
            IsAuthenticated = executionContext.IsAuthenticated,
            Language = executionContext.Language,
            SessionId = executionContext.SessionId,
            StartDate = DateTime.UtcNow,
            TraceId = executionContext.TraceId,
            UniqueId = executionContext.UniqueId,
            UserId = executionContext.UserId,
            UserIp = executionContext.UserIp,
            Username = executionContext.Username
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
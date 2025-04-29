namespace FluentCMS.Core.Plugins.History;

public class EntityHistoryEventHandler<T>(IEntityHistoryRepository entityHistoryRepository, ILogger<EntityHistoryEventHandler<T>> logger) : IEventSubscriber<T> where T : class, IAuditableEntity
{
    public async Task Handle(DomainEvent<T> domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        //// Skip handling if T is EntityHistory<>
        //if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(EntityHistory<>))
        //    return;

        try
        {
            await entityHistoryRepository.Add(domainEvent.Data, domainEvent.EventType, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to record history for {EntityType} with ID {EntityId}", typeof(T).Name, domainEvent.Data.Id);

            // TODO: should we throw?
            throw;
        }
    }
}

namespace FluentCMS.Core.Plugins.History;

public class EntityHistoryEventHandler<T>(IEntityHistoryRepository<T> historyRepository, ILogger<EntityHistoryEventHandler<T>> logger) : IEventSubscriber<T> where T : IBaseEntity
{
    public async Task Handle(DomainEvent<T> domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await historyRepository.Add(domainEvent.Data, domainEvent.EventType, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to record history for {EntityType} with ID {EntityId}", typeof(T).Name, domainEvent.Data.Id);
            throw;
        }
    }
}

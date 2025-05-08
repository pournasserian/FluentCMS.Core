using FluentCMS.Plugins.AuditTrailManager.Services;

namespace FluentCMS.Plugins.AuditTrailManager.Handlers;

public class AuditTrailHandler<T>(IAuditTrailService service, ILogger<AuditTrailHandler<T>> logger) : IEventSubscriber<T>
{
    public async Task Handle(DomainEvent<T> domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Check if event data is null
            if (domainEvent.Data is null)
            {
                // Throw an exception or handle it as per your requirements
                throw new ArgumentNullException("Event data cannot be null.");
            }

            var entityTypeName = domainEvent.Data.GetType().Name;
            var validEventTypes = new[] { $"{entityTypeName}.Adding", $"{entityTypeName}.Updated", $"{entityTypeName}.Removed" };

            if (validEventTypes.Contains(domainEvent.EventType))
                await service.Add(domainEvent.Data, domainEvent.EventType, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to record AuditTrail for {EntityType}", typeof(T).Name);

            // TODO: should we throw?
            throw;
        }
    }
}

namespace FluentCMS.Core.EventBus;

public class EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger) : IEventPublisher
{
    public async Task Publish<T>(T data, string eventType, CancellationToken cancellationToken = default)
    {
        var domainEvent = new DomainEvent<T>(data, eventType);
        await Publish(domainEvent, cancellationToken);
    }

    public async Task Publish<T>(DomainEvent<T> domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get all subscribers for this event type
            var subscribers = serviceProvider.GetServices<IEventSubscriber<T>>();

            if (!subscribers.Any())
            {
                logger.LogWarning("No subscribers found for event type {EventType}", domainEvent.EventType);
                return;
            }

            // Execute all handlers in parallel
            var tasks = subscribers.Select(subscriber =>
                subscriber.Handle(domainEvent, cancellationToken));

            await Task.WhenAll(tasks);

            logger.LogInformation("Event {EventType} published successfully with {SubscriberCount} subscribers",
                domainEvent.EventType, subscribers.Count());
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Event publishing was cancelled for event type {EventType}", domainEvent.EventType);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing event {EventType}", domainEvent.EventType);
            throw;
        }
    }
}

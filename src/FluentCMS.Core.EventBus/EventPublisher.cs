namespace FluentCMS.Core.EventBus;

public class EventPublisher(IServiceProvider serviceProvider) : IEventPublisher
{
    protected readonly IServiceProvider ServiceProvider = serviceProvider ??
        throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ILogger<EventPublisher> Logger = serviceProvider.GetService<ILogger<EventPublisher>>() ??
        throw new ArgumentNullException(nameof(Logger));

    public async Task Publish<TEvent>(TEvent data, CancellationToken cancellationToken = default) where TEvent : class, IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(data);

        // Get all registered subscribers for this event type
        var subscribers = ServiceProvider.GetServices<IEventSubscriber<TEvent>>().ToList();

        if (subscribers.Count == 0)
            return;

        // Create a list to hold any exceptions that occur during handler execution
        var exceptions = new List<Exception>();

        // Execute all handlers
        var tasks = subscribers.Select(async subscriber =>
        {
            try
            {
                await subscriber.Handle(data, cancellationToken);
            }
            catch (Exception ex)
            {
                // Collect exceptions but don't stop other handlers from executing
                exceptions.Add(ex);
            }
        });

        // Wait for all handlers to complete
        await Task.WhenAll(tasks);

        // If any handlers threw exceptions, throw an aggregate exception
        if (exceptions.Count != 0)
        {
            // Log the exceptions here if needed
            foreach (var exception in exceptions)
                Logger.LogError(exception, "An error occurred while handling the event.");

            throw new AggregateException("One or more event handlers threw an exception.", exceptions);
        }
    }
}


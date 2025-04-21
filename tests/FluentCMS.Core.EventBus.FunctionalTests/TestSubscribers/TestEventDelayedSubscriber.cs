namespace FluentCMS.Core.EventBus.FunctionalTests.TestSubscribers;

// Subscriber that simulates a delayed processing operation
public class TestEventDelayedSubscriber : IEventSubscriber<TestEvent>
{
    private readonly ILogger<TestEventDelayedSubscriber> _logger;
    private readonly int _delayMilliseconds;
    private readonly List<TestEvent> _receivedEvents = new();

    public TestEventDelayedSubscriber(ILogger<TestEventDelayedSubscriber> logger, int delayMilliseconds = 500)
    {
        _logger = logger;
        _delayMilliseconds = delayMilliseconds;
    }

    public async Task Handle(DomainEvent<TestEvent> domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Delayed subscriber beginning processing for event: {EventType}",
            domainEvent.EventType);

        // Simulate a long-running process
        await Task.Delay(_delayMilliseconds, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        lock (_receivedEvents)
        {
            _receivedEvents.Add(domainEvent.Data);
        }

        _logger.LogInformation("Delayed subscriber finished processing for event: {EventType}",
            domainEvent.EventType);
    }

    // Helper methods for tests
    public IReadOnlyList<TestEvent> GetReceivedEvents()
    {
        lock (_receivedEvents)
        {
            return _receivedEvents.ToList().AsReadOnly();
        }
    }

    public void ClearEvents()
    {
        lock (_receivedEvents)
        {
            _receivedEvents.Clear();
        }
    }
}

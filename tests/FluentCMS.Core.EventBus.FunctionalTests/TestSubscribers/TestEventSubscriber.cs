namespace FluentCMS.Core.EventBus.FunctionalTests.TestSubscribers;

// Simple subscriber for TestEvent
public class TestEventSubscriber : IEventSubscriber<TestEvent>
{
    private readonly ILogger<TestEventSubscriber> _logger;
    private readonly List<TestEvent> _receivedEvents = new();

    public TestEventSubscriber(ILogger<TestEventSubscriber> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEvent<TestEvent> domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Received event: {EventType} with message: {Message}",
            domainEvent.EventType, domainEvent.Data.Message);

        _receivedEvents.Add(domainEvent.Data);

        return Task.CompletedTask;
    }

    // Helper methods for tests
    public IReadOnlyList<TestEvent> GetReceivedEvents() => _receivedEvents.AsReadOnly();

    public void ClearEvents() => _receivedEvents.Clear();

    public bool HasReceivedEvent(Guid id) => _receivedEvents.Any(e => e.Id == id);
}

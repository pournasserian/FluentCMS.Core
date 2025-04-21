using FluentCMS.Core.EventBus.FunctionalTests.TestEvents;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Core.EventBus.FunctionalTests.TestSubscribers;

// Subscriber that throws exceptions to test error handling
public class TestEventFailingSubscriber : IEventSubscriber<TestEvent>
{
    private readonly ILogger<TestEventFailingSubscriber> _logger;
    private readonly bool _alwaysFail;
    private readonly string _failureMessage;
    private int _processedCount = 0;
    private int _failedCount = 0;

    public TestEventFailingSubscriber(
        ILogger<TestEventFailingSubscriber> logger,
        bool alwaysFail = false,
        string failureMessage = "Simulated failure in event handler")
    {
        _logger = logger;
        _alwaysFail = alwaysFail;
        _failureMessage = failureMessage;
    }

    public Task Handle(DomainEvent<TestEvent> domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Failing subscriber received event: {EventType}", domainEvent.EventType);

        Interlocked.Increment(ref _processedCount);

        // Fail on odd count or if configured to always fail
        if (_alwaysFail || _processedCount % 2 != 0)
        {
            _logger.LogError("Failing subscriber is throwing exception for event: {EventType}", domainEvent.EventType);
            Interlocked.Increment(ref _failedCount);
            throw new InvalidOperationException(_failureMessage);
        }

        return Task.CompletedTask;
    }

    // Helper methods for tests
    public int GetProcessedCount() => _processedCount;

    public int GetFailedCount() => _failedCount;

    public void Reset()
    {
        _processedCount = 0;
        _failedCount = 0;
    }
}

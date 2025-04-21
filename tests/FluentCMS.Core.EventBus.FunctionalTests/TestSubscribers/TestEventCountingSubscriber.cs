using FluentCMS.Core.EventBus.FunctionalTests.TestEvents;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Core.EventBus.FunctionalTests.TestSubscribers;

// Subscriber that counts events and provides aggregate statistics
public class TestEventCountingSubscriber : IEventSubscriber<TestEvent>
{
    private readonly ILogger<TestEventCountingSubscriber> _logger;
    private int _eventCount = 0;
    private readonly object _lock = new();

    // For tracking execution timing
    private DateTime? _firstEventTime;
    private DateTime? _lastEventTime;

    public TestEventCountingSubscriber(ILogger<TestEventCountingSubscriber> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEvent<TestEvent> domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _eventCount++;
            _lastEventTime = DateTime.UtcNow;

            if (_firstEventTime == null)
            {
                _firstEventTime = _lastEventTime;
            }
        }

        _logger.LogInformation("Event counter received event: {EventType}. Total count: {Count}",
            domainEvent.EventType, _eventCount);

        return Task.CompletedTask;
    }

    // Helper methods for tests
    public int GetEventCount()
    {
        lock (_lock)
        {
            return _eventCount;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _eventCount = 0;
            _firstEventTime = null;
            _lastEventTime = null;
        }
    }

    public TimeSpan? GetProcessingDuration()
    {
        lock (_lock)
        {
            if (_firstEventTime == null || _lastEventTime == null)
                return null;

            return _lastEventTime.Value - _firstEventTime.Value;
        }
    }
}

namespace FluentCMS.Core.EventBus.FunctionalTests.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task EventBus_CompleteIntegration_ShouldWorkAsExpected()
    {
        // Arrange - create a service collection similar to a real application
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(configure => configure
            .AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        // Add EventBus
        services.AddEventBus();

        // Register test subscribers
        services.AddSingleton<TestEventSubscriber>();
        services.AddSingleton<TestEventCountingSubscriber>();
        services.AddSingleton<TestEventDelayedSubscriber>();

        // Register subscribers to the event bus
        services.AddSingleton<IEventSubscriber<TestEvent>>(sp =>
            sp.GetRequiredService<TestEventSubscriber>());
        services.AddSingleton<IEventSubscriber<TestEvent>>(sp =>
            sp.GetRequiredService<TestEventCountingSubscriber>());
        services.AddSingleton<IEventSubscriber<TestEvent>>(sp =>
            sp.GetRequiredService<TestEventDelayedSubscriber>());

        // Register subscribers for payload events
        services.AddSingleton<PayloadSubscriber>();
        services.AddSingleton<IEventSubscriber<TestEventWithPayload>>(sp =>
            sp.GetRequiredService<PayloadSubscriber>());

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get services
        var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();
        var simpleSubscriber = serviceProvider.GetRequiredService<TestEventSubscriber>();
        var countingSubscriber = serviceProvider.GetRequiredService<TestEventCountingSubscriber>();
        var delayedSubscriber = serviceProvider.GetRequiredService<TestEventDelayedSubscriber>();
        var payloadSubscriber = serviceProvider.GetRequiredService<PayloadSubscriber>();

        // Create test events
        var simpleEvent = new TestEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Message = "Integration Test Event"
        };

        var payloadEvent = new TestEventWithPayload
        {
            Id = Guid.NewGuid(),
            EventName = "Integration Test Payload Event",
            Data = new TestEventWithPayload.Payload
            {
                Number = 42,
                Text = "Integration Test",
                IsActive = true,
                Items = new List<string> { "Test Item 1", "Test Item 2" }
            }
        };

        // Act - publish both event types
        await eventPublisher.Publish(simpleEvent, "IntegrationTest.Simple");
        await eventPublisher.Publish(payloadEvent, "IntegrationTest.Payload");

        // Wait for delayed subscriber to complete
        await Task.Delay(600);

        // Assert - verify all subscribers received the appropriate events
        simpleSubscriber.GetReceivedEvents().Should().ContainSingle();
        simpleSubscriber.GetReceivedEvents().First().Id.Should().Be(simpleEvent.Id);

        countingSubscriber.GetEventCount().Should().Be(1);

        delayedSubscriber.GetReceivedEvents().Should().ContainSingle();
        delayedSubscriber.GetReceivedEvents().First().Id.Should().Be(simpleEvent.Id);

        payloadSubscriber.GetReceivedEvents().Should().ContainSingle();
        payloadSubscriber.GetReceivedEvents().First().Id.Should().Be(payloadEvent.Id);

        // Cleanup
        (serviceProvider as IDisposable)?.Dispose();
    }

    // Payload subscriber for integration test
    private class PayloadSubscriber : IEventSubscriber<TestEventWithPayload>
    {
        private readonly ILogger<PayloadSubscriber> _logger;
        private readonly List<TestEventWithPayload> _receivedEvents = new();

        public PayloadSubscriber(ILogger<PayloadSubscriber> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEvent<TestEventWithPayload> domainEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Payload subscriber received: {EventType} with payload {Number} - {Text}",
                domainEvent.EventType, domainEvent.Data.Data.Number, domainEvent.Data.Data.Text);

            lock (_receivedEvents)
            {
                _receivedEvents.Add(domainEvent.Data);
            }

            return Task.CompletedTask;
        }

        public IReadOnlyList<TestEventWithPayload> GetReceivedEvents()
        {
            lock (_receivedEvents)
            {
                return _receivedEvents.ToList().AsReadOnly();
            }
        }
    }
}

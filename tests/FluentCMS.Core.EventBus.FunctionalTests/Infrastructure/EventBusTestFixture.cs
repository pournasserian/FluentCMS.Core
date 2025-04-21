namespace FluentCMS.Core.EventBus.FunctionalTests.Infrastructure;

// Test fixture that provides a configured event bus for testing
public class EventBusTestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private bool _disposed;

    public IEventPublisher EventPublisher { get; }
    public IServiceProvider ServiceProvider => _serviceProvider;

    public EventBusTestFixture()
    {
        // Build a service collection with event publisher and test subscribers
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(configure =>
            configure.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Register the event bus
        services.AddEventBus();

        // Register our test subscribers
        services.AddScoped<TestEventSubscriber>();
        services.AddScoped<TestEventCountingSubscriber>();
        services.AddScoped<TestEventDelayedSubscriber>();
        services.AddScoped<TestEventFailingSubscriber>();

        // Register our subscribers with the event bus system
        services.AddSingleton<IEventSubscriber<TestEvent>>(sp =>
            sp.GetRequiredService<TestEventSubscriber>());
        services.AddSingleton<IEventSubscriber<TestEvent>>(sp =>
            sp.GetRequiredService<TestEventCountingSubscriber>());

        // Build the service provider
        _serviceProvider = services.BuildServiceProvider();

        // Get the event publisher
        EventPublisher = _serviceProvider.GetRequiredService<IEventPublisher>();
    }

    // Helper methods for tests

    // Get a subscriber instance
    public T GetSubscriber<T>() where T : class => _serviceProvider.GetRequiredService<T>();

    // Create a test event
    public TestEvent CreateTestEvent(string message = "Test Message")
    {
        return new TestEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Message = message
        };
    }

    // Create a test event with payload
    public TestEventWithPayload CreateTestEventWithPayload(int number = 42, string text = "Test Text", bool isActive = true)
    {
        return new TestEventWithPayload
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            EventName = "Test Complex Event",
            Data = new TestEventWithPayload.Payload
            {
                Number = number,
                Text = text,
                IsActive = isActive,
                Items = new List<string> { "Item 1", "Item 2", "Item 3" }
            }
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _serviceProvider.Dispose();
        }

        _disposed = true;
    }
}

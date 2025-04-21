namespace FluentCMS.Core.EventBus.FunctionalTests.TestEvents;

// Simple test event data class
public class TestEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = "Default test message";
}

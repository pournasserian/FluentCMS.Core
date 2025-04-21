namespace FluentCMS.Core.EventBus.FunctionalTests.TestEvents;

// Test event with a more complex payload
public class TestEventWithPayload
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventName { get; set; } = "Complex Event";
    public Payload Data { get; set; } = new Payload();
    
    public class Payload
    {
        public int Number { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> Items { get; set; } = new List<string>();
    }
}

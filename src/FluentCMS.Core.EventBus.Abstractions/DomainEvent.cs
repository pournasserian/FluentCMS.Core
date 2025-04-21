namespace FluentCMS.Core.EventBus.Abstractions;

// Generic domain event
public class DomainEvent<T>(T data, string eventType) : IEvent
{
    public T Data { get; } = data;
    public string EventType { get; } = eventType;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

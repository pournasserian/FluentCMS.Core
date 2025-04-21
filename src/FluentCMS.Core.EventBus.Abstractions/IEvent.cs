namespace FluentCMS.Core.EventBus.Abstractions;

// Base event interface
public interface IEvent
{
    DateTime Timestamp { get; }
}

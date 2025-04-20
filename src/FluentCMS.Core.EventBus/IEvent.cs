namespace FluentCMS.Core.EventBus;

// Base event interface
public interface IEvent
{
    DateTime Timestamp { get; }
}

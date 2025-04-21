namespace FluentCMS.Core.EventBus.Abstractions;

// Generic event publisher interface
public interface IEventPublisher
{
    Task Publish<T>(T data, string eventType, CancellationToken cancellationToken = default);
    Task Publish<T>(DomainEvent<T> domainEvent, CancellationToken cancellationToken = default);
}

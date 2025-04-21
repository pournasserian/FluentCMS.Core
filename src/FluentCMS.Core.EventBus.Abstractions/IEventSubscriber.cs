namespace FluentCMS.Core.EventBus.Abstractions;

// Generic event subscriber interface
public interface IEventSubscriber<T>
{
    Task Handle(DomainEvent<T> domainEvent, CancellationToken cancellationToken = default);
}

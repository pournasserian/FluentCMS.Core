namespace FluentCMS.Repositories.Abstractions;

public interface IRepositortyEventPublisher
{
    Task PublishCreated(RepositoryEntityCreatedEvent repsitoryEvent, CancellationToken cancellationToken = default);
    Task PublishUpdated(RepositoryEntityUpdatedEvent repsitoryEvent, CancellationToken cancellationToken = default);
    Task PublishRemoved(RepositoryEntityRemovedEvent repsitoryEvent, CancellationToken cancellationToken = default);
}
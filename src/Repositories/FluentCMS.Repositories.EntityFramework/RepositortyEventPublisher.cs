namespace FluentCMS.Repositories.EntityFramework;

public class RepositortyEventPublisher(IEventPublisher eventPublisher) : IRepositortyEventPublisher
{
    public async Task PublishCreated(RepositoryEntityCreatedEvent repsitoryEvent, CancellationToken cancellationToken = default)
    {
        await eventPublisher.Publish(repsitoryEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishRemoved(RepositoryEntityRemovedEvent repsitoryEvent, CancellationToken cancellationToken = default)
    {
        await eventPublisher.Publish(repsitoryEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishUpdated(RepositoryEntityUpdatedEvent repsitoryEvent, CancellationToken cancellationToken = default)
    {
        await eventPublisher.Publish(repsitoryEvent, cancellationToken).ConfigureAwait(false);
    }
}
namespace FluentCMS.Repositories.EntityFramework.Interceptors;

public class RepositoryEventBusPublisherInterceptor(IServiceProvider serviceProvider, ILogger<RepositoryEventBusPublisherInterceptor> logger) : BaseSaveChangesInterceptor<IEventPublisherDbContext>
{
    public override async Task AfterSaveChanges(DbContextEventData eventData , CancellationToken cancellationToken = default)
    {
        var publisher = serviceProvider.GetService<IRepositortyEventPublisher>();
        if (publisher == null)
        {
            // Log error: IEventPublisher not found in DI container
            logger.LogError("IRepositortyEventPublisher not found in DI container.");
            return;
        }

        foreach (var entry in eventData.Context!.ChangeTracker.Entries<IEntity>())
        {
            var entity = entry.Entity;
            switch (entry.State)
            {
                case EntityState.Added:
                    await publisher.PublishCreated(RepositoryEntityCreatedEvent.Create(entity), cancellationToken).ConfigureAwait(false);
                    break;
                case EntityState.Modified:
                    await publisher.PublishUpdated(RepositoryEntityUpdatedEvent.Create(entity), cancellationToken).ConfigureAwait(false);
                    break;
                case EntityState.Deleted:
                    await publisher.PublishRemoved(RepositoryEntityRemovedEvent.Create(entity), cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }
}
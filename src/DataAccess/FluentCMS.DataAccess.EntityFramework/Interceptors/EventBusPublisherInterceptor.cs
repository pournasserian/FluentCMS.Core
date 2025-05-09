using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FluentCMS.DataAccess.EntityFramework.Interceptors;

public class EventBusPublisherInterceptor(IRepositortyEventPublisher publisher) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        var saveChangeResult = base.SavingChanges(eventData, result);
        AfterSaveChanges(eventData.Context).Wait();
        return saveChangeResult;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var saveChangeResult = await base.SavingChangesAsync(eventData, result, cancellationToken);
        await AfterSaveChanges(eventData.Context, cancellationToken);
        return saveChangeResult;
    }

    private async Task AfterSaveChanges(DbContext? context, CancellationToken cancellationToken = default)
    {
        // Check if dbContext implements IEventPublisherDbContext
        if (context == null || context is not IEventPublisherDbContext) 
            return;

        foreach (var entry in context.ChangeTracker.Entries<IEntity>())
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
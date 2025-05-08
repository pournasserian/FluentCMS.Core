using FluentCMS.Core.EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FluentCMS.DataAccess.EntityFramework.Interceptors;

public class EventBusInterceptor(IEventPublisher eventPublisher) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        BeforeSaveChanges(eventData.Context).Wait();
        var saveChangeResult = base.SavingChanges(eventData, result);
        AfterSaveChanges(eventData.Context).Wait();
        return saveChangeResult;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await BeforeSaveChanges(eventData.Context, cancellationToken);
        var saveChangeResult = await base.SavingChangesAsync(eventData, result, cancellationToken);
        await AfterSaveChanges(eventData.Context, cancellationToken);
        return saveChangeResult;
    }

    private async Task BeforeSaveChanges(DbContext? context, CancellationToken cancellationToken = default)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<IEntity>())
        {
            var entity = entry.Entity;
            switch (entry.State)
            {
                case EntityState.Added:
                    await eventPublisher.Publish(entity, $"{entity.GetType().Name}.Adding", cancellationToken).ConfigureAwait(false);
                    break;

                case EntityState.Modified:
                    await eventPublisher.Publish(entity, $"{entity.GetType().Name}.Updating", cancellationToken).ConfigureAwait(false);
                    break;

                case EntityState.Deleted:
                    await eventPublisher.Publish(entity, $"{entity.GetType().Name}.Removing", cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }

    private async Task AfterSaveChanges(DbContext? context, CancellationToken cancellationToken = default)
    {
        if (context == null) return;
        foreach (var entry in context.ChangeTracker.Entries<IEntity>())
        {
            var entity = entry.Entity;
            switch (entry.State)
            {
                case EntityState.Added:
                    await eventPublisher.Publish(entity, $"{entity.GetType().Name}.Added", cancellationToken).ConfigureAwait(false);
                    break;
                case EntityState.Modified:
                    await eventPublisher.Publish(entity, $"{entity.GetType().Name}.Updated", cancellationToken).ConfigureAwait(false);
                    break;
                case EntityState.Deleted:
                    await eventPublisher.Publish(entity, $"{entity.GetType().Name}.Removed", cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }
}
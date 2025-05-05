using FluentCMS.Core.EventBus.Abstractions;
using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FluentCMS.DataAccess.EntityFramework;

public class EventBusSaveChangesInterceptor(IEventPublisher eventPublisher) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditInformation(eventData.Context).Wait();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await ApplyAuditInformation(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task ApplyAuditInformation(DbContext? context, CancellationToken cancellationToken = default)
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

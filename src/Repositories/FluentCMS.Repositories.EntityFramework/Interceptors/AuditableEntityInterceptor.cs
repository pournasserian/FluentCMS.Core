namespace FluentCMS.Repositories.EntityFramework.Interceptors;

public class AuditableEntityInterceptor(IApplicationExecutionContext executionContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        BeforeSaveChanges(eventData.Context);
        var saveChangeResult = base.SavingChanges(eventData, result);
        return saveChangeResult;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        BeforeSaveChanges(eventData.Context, cancellationToken);
        var saveChangeResult = await base.SavingChangesAsync(eventData, result, cancellationToken);
        return saveChangeResult;
    }

    private void BeforeSaveChanges(DbContext? context, CancellationToken cancellationToken = default)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            var entity = entry.Entity;
            switch (entry.State)
            {
                case EntityState.Added:
                    // For new entities, set the CreatedAt and CreatedBy properties
                    // Also set the Version to 1
                    entity.CreatedBy = executionContext.Username;
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.Version = 1;
                    break;

                case EntityState.Modified:
                    // For modified entities, set the UpdatedAt and UpdatedBy properties
                    // Also increment the Version
                    entity.UpdatedBy = executionContext.Username;
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.Version++;
                    
                    break;

                case EntityState.Deleted:
                    // Do nothing                    
                    break;
            }
        }
    }
}
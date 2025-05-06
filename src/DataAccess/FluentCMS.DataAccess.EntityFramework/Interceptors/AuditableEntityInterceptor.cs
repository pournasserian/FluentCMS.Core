using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using FluentCMS.DataAccess.Abstractions;

namespace FluentCMS.DataAccess.EntityFramework.Interceptors;

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

        foreach (var entry in context.ChangeTracker.Entries<IEntity>())
        {
            var entity = entry.Entity;
            switch (entry.State)
            {
                case EntityState.Added:
                    // For new entities, set the CreatedAt and CreatedBy properties
                    // and generate a new Guid for the Id if it is empty
                    // Also set the Version to 1
                    if (entity is IAuditableEntity idEntity && idEntity.Id == Guid.Empty)
                        idEntity.Id = Guid.NewGuid();

                    if (entity is IAuditableEntity auditableAdding)
                    {
                        auditableAdding.CreatedBy = executionContext.Username;
                        auditableAdding.CreatedAt = DateTime.UtcNow;
                        auditableAdding.Version = 1;
                    }
                    break;

                case EntityState.Modified:
                    if (entity is IAuditableEntity auditableUpdating)
                    {
                        // For modified entities, increment the version
                        // The current value is the original value from when the entity was loaded
                        var originalVersion = entry.OriginalValues.GetValue<int>(nameof(IAuditableEntity.Version));
                        var currentVersion = auditableUpdating.Version;

                        // If the version hasn't changed from original, increment it
                        // If it has changed, it means someone manually set it, which might indicate a problem
                        if (originalVersion != currentVersion)
                        {
                            throw new RepositoryException($"Version mismatch for entity {entity.GetType().Name}. Original: {originalVersion}, Current: {currentVersion}.");
                        }

                        auditableUpdating.UpdatedBy = executionContext.Username;
                        auditableUpdating.UpdatedAt = DateTime.UtcNow;
                        auditableUpdating.Version++;
                    }
                    break;

                case EntityState.Deleted:
                    if (entity is IAuditableEntity auditableRemoving)
                    {
                        // For modified entities, increment the version
                        // The current value is the original value from when the entity was loaded
                        var originalVersion = entry.OriginalValues.GetValue<int>(nameof(IAuditableEntity.Version));
                        var currentVersion = auditableRemoving.Version;

                        // If the version hasn't changed from original, increment it
                        // If it has changed, it means someone manually set it, which might indicate a problem
                        if (originalVersion != currentVersion)
                        {
                            throw new RepositoryException($"Version mismatch for entity {entity.GetType().Name}. Original: {originalVersion}, Current: {currentVersion}.");
                        }
                        auditableRemoving.Version++;
                    }
                    break;
            }
        }
    }
}
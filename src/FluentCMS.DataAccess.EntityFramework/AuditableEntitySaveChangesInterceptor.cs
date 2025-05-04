using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FluentCMS.DataAccess.EntityFramework;

/// <summary>
/// Interceptor that automatically sets audit properties (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, Version)
/// for entities implementing IAuditableEntity during SaveChanges operations.
/// </summary>
public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IApplicationExecutionContext _executionContext;

    public AuditableEntitySaveChangesInterceptor(IApplicationExecutionContext executionContext)
    {
        _executionContext = executionContext;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditableEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        SetAuditPropertiesForNewEntity(auditableEntity, entry);
                        break;
                    case EntityState.Modified:
                        SetAuditPropertiesForModifiedEntity(auditableEntity, entry);
                        break;
                }
            }
        }
    }

    private void SetAuditPropertiesForNewEntity(IAuditableEntity entity, EntityEntry entry)
    {
        // Set creation timestamp to current UTC time
        entity.CreatedAt = DateTime.UtcNow;
        
        // Set creator information if available from execution context
        if (_executionContext.IsAuthenticated)
        {
            entity.CreatedBy = !string.IsNullOrEmpty(_executionContext.Username) 
                ? _executionContext.Username 
                : _executionContext.UserId?.ToString();
        }

        // Initialize version
        entity.Version = 1;
        
        // Make sure UpdatedAt and UpdatedBy are initially null for new entities
        entity.UpdatedAt = null;
        entity.UpdatedBy = null;
    }

    private void SetAuditPropertiesForModifiedEntity(IAuditableEntity entity, EntityEntry entry)
    {
        // Don't modify CreatedAt and CreatedBy on updates
        entry.Property("CreatedAt").IsModified = false;
        entry.Property("CreatedBy").IsModified = false;
        
        // Set update timestamp to current UTC time
        entity.UpdatedAt = DateTime.UtcNow;
        
        // Set updater information if available from execution context
        if (_executionContext.IsAuthenticated)
        {
            entity.UpdatedBy = !string.IsNullOrEmpty(_executionContext.Username) 
                ? _executionContext.Username 
                : _executionContext.UserId?.ToString();
        }
        
        // Increment version
        entity.Version++;
    }
}

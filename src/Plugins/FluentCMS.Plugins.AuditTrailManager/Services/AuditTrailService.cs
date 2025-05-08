using FluentCMS.Plugins.AuditTrailManager.Repositories;

namespace FluentCMS.Plugins.AuditTrailManager.Services;

public interface IAuditTrailService
{
    Task Add(object entity, string eventType, CancellationToken cancellationToken = default);
}

public class AuditTrailService(IAuditTrailRepository repository, IApplicationExecutionContext executionContext) : IAuditTrailService
{
    public async Task Add(object entity, string eventType, CancellationToken cancellationToken = default)
    {
        if (entity is IAuditableEntity auditableEntity)
        {
            var auditTrail = new AuditTrail
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Entity = auditableEntity,
                Context = executionContext,
            };
            await repository.Add(auditTrail, cancellationToken);
        }
    }
}

namespace FluentCMS.Plugins.AuditTrailManager.Handlers;

public class AuditTrailHandler<T>(IAuditTrailUnitOfWork uow, IApplicationExecutionContext executionContext, ILogger<AuditTrailHandler<T>> logger) : IEventSubscriber<T>
{
    public async Task Handle(DomainEvent<T> domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Skip handling if T is not IAuditableEntity
            if (domainEvent.Data is IAuditableEntity auditableEntity)
            {
                var auditTrail = new AuditTrail
                {
                    EntityId = auditableEntity.Id,
                    EntityType = typeof(T).Name,
                    EventType = domainEvent.EventType,
                    Timestamp = DateTime.UtcNow,
                    Entity = auditableEntity,
                    IsAuthenticated = executionContext.IsAuthenticated,
                    Language = executionContext.Language,
                    SessionId = executionContext.SessionId,
                    StartDate = DateTime.UtcNow,
                    TraceId = executionContext.TraceId,
                    UniqueId = executionContext.UniqueId,
                    UserId = executionContext.UserId,
                    UserIp = executionContext.UserIp,
                    Username = executionContext.Username
                };

                await uow.AuditTrails.Add(auditTrail, cancellationToken);
                await uow.SaveChanges(cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to record AuditTrail for {EntityType}", typeof(T).Name);

            // TODO: should we throw?
            throw;
        }
    }
}

namespace FluentCMS.Plugins.AuditTrailManager.Repositories;

public interface IAuditTrailRepository
{
    Task<AuditTrail> Add(AuditTrail entity, CancellationToken cancellationToken = default);
}

public class AuditTrailRepository(AuditTrailDbContext auditTrailDbContext, IMapper mapper) : IAuditTrailRepository
{
    public async Task<AuditTrail> Add(AuditTrail entity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var internalEntity = mapper.Map<AuditTrailInternal>(entity);
        await auditTrailDbContext.AuditTrails.AddAsync(internalEntity, cancellationToken);
        await auditTrailDbContext.SaveChangesAsync(cancellationToken);

        return mapper.Map<AuditTrail>(entity);
    }
}
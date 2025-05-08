namespace FluentCMS.Plugins.AuditTrailManager.Repositories;

public interface IAuditTrailRepository : IRepository<AuditTrail>
{
}

public class AuditTrailRepository : Repository<AuditTrail, AuditTrailDbContext>, IAuditTrailRepository
{
    public AuditTrailRepository(AuditTrailDbContext auditTrailDbContext) : base(auditTrailDbContext)
    {

    }
}
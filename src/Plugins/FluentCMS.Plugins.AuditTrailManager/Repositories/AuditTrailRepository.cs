namespace FluentCMS.Plugins.AuditTrailManager.Repositories;

public interface IAuditTrailRepository : IRepository<AuditTrail>
{
}

public class AuditTrailRepository : Repository<AuditTrail, AuditTrailDbContext>, IAuditTrailRepository
{
    public AuditTrailRepository(AuditTrailDbContext auditTrailDbContext) : base(auditTrailDbContext)
    {
        // Log the context creation
        Console.WriteLine("AuditTrailRepository created");
        Console.WriteLine("AuditTrailRepository created with context: {0}", auditTrailDbContext.GetType().Name);
        Console.WriteLine("AuditTrailRepository created with DbSet: {0}", auditTrailDbContext.Database.ProviderName);

    }
}
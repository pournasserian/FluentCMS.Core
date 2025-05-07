using FluentCMS.Plugins.AuditTrailManager.Repositories;

namespace FluentCMS.Plugins.AuditTrailManager.Models;

public interface IAuditTrailUnitOfWork : IUnitOfWork
{
    IAuditTrailRepository AuditTrails { get; }

}

public class AuditTrailUnitOfWork : UnitOfWork<AuditTrailDbContext>, IAuditTrailUnitOfWork
{
    public AuditTrailUnitOfWork(AuditTrailDbContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {

    }
    public IAuditTrailRepository AuditTrails => (IAuditTrailRepository)Repository<AuditTrail>();
}

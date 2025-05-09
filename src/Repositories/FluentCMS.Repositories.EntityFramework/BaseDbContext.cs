namespace FluentCMS.Repositories.EntityFramework;

public abstract class BaseDbContext : DbContext, IEventPublisherDbContext
{
    public BaseDbContext(DbContextOptions options) : base(options)
    {
        ChangeTracker.AutoDetectChangesEnabled = false;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}
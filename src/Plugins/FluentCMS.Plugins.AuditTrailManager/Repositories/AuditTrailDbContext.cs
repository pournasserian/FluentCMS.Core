namespace FluentCMS.Plugins.AuditTrailManager.Repositories;

public class AuditTrailDbContext : DbContext
{
    public AuditTrailDbContext(DbContextOptions<AuditTrailDbContext> options, ILogger<AuditTrailDbContext> logger) : base(options)
    {
        logger.LogDebug("AuditTrailDbContext created");
        logger.LogDebug("AuditTrailDbContext created with context: {Context}", options.ContextType.Name);
        logger.LogDebug("AuditTrailDbContext created with DbSet: {ProviderName}", Database.ProviderName);
    }
    public DbSet<AuditTrail> AuditTrails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditTrail>().ToTable("AuditTrails");

        modelBuilder.Entity<AuditTrail>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<AuditTrail>()
         .Property(a => a.EntityJson);

        //// Seed initial data
        //modelBuilder.Entity<AuditTrail>().HasData(
        //    new AuditTrail
        //    {
        //        Id = Guid.NewGuid(),
        //        EntityId = Guid.NewGuid(),
        //        EntityType = "Todo",
        //        EventType = "Created",
        //        Timestamp = DateTime.UtcNow
        //    });
    }
}
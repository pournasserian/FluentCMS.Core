namespace FluentCMS.Plugins.AuditTrailManager.Models;

public class AuditTrailDbContext : DbContext
{
    public AuditTrailDbContext(DbContextOptions<AuditTrailDbContext> options) : base(options)
    {

    }
    public DbSet<AuditTrail> AuditTrails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditTrail>().ToTable("AuditTrails");

        modelBuilder.Entity<AuditTrail>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<AuditTrail>()
         .Property(a => a.EntityJson);

        // Seed initial data
        modelBuilder.Entity<AuditTrail>().HasData(
            new AuditTrail
            {
                Id = Guid.NewGuid(),
                EntityId = Guid.NewGuid(),
                EntityType = "Todo",
                EventType = "Created",
                Timestamp = DateTime.UtcNow
            });
    }
}
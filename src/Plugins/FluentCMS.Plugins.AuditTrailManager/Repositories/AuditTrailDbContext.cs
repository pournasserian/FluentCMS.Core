namespace FluentCMS.Plugins.AuditTrailManager.Repositories;

public class AuditTrailDbContext(DbContextOptions<AuditTrailDbContext> options) : DbContext(options)
{
    public DbSet<AuditTrailInternal> AuditTrails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(nameof(AuditTrailDbContext));

        modelBuilder.Entity<AuditTrailInternal>()
            .ToTable("AuditTrails");

        modelBuilder.Entity<AuditTrailInternal>()
            .HasKey(t => t.Id);
    }
}
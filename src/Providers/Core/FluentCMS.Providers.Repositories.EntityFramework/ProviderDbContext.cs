using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Providers.Repositories.EntityFramework;

/// <summary>
/// Entity Framework database context for the provider system.
/// </summary>
public class ProviderDbContext(DbContextOptions<ProviderDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Provider instances stored in the database.
    /// </summary>
    public DbSet<Provider> Providers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Provider entity
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Area)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.ModuleType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Options)
                .IsRequired();

        });
    }
}

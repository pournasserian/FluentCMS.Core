using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Configuration;

public class OptionsDbContext(DbContextOptions<OptionsDbContext> options) : DbContext(options)
{
    public DbSet<OptionsEntity> Options { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OptionsEntity>(entity =>
        {
            entity.HasIndex(e => e.TypeName).IsUnique();
        });
    }
}
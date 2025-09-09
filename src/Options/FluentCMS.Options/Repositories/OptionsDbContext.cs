using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Options.Repositories;

public class OptionsDbContext(DbContextOptions<OptionsDbContext> options) : DbContext(options)
{
    public DbSet<OptionsDbModel> Options { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OptionsDbModel>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<OptionsDbModel>()
            .Property(t => t.Alias)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<OptionsDbModel>()
            .Property(t => t.Type)
            .IsRequired();

        modelBuilder.Entity<OptionsDbModel>()
            .HasIndex(x => new { x.Alias, x.Type })
            .IsUnique();

        modelBuilder.Entity<OptionsDbModel>()
            .Property(t => t.Value)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(4000);
    }
}
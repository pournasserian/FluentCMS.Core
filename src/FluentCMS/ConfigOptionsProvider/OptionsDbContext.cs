using FluentCMS.Repositories.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.ConfigOptionsProvider;

// DbContext for accessing the SQLite database
public class OptionsDbContext(DbContextOptions<OptionsDbContext> options) : BaseDbContext(options)
{
    public DbSet<OptionEntry> Options { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OptionEntry>()
            .ToTable("Options")
            .HasIndex(o => o.TypeName)
            .IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FluentCMS.Configuration.EntityFramework;

public class ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options, IConfiguration configuration) : DbContext(options)
{
    private readonly IConfigurationRoot? _configurationRoot = configuration as IConfigurationRoot;

    public DbSet<ConfigurationSetting> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ConfigurationSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        _configurationRoot?.Reload();
        return result;
    }

    public override int SaveChanges()
    {
        var result = base.SaveChanges();
        _configurationRoot?.Reload();
        return result;
    }
}
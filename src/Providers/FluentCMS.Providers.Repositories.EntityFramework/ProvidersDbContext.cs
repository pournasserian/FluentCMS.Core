using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Providers.Repositories.EntityFramework;

public class ProvidersDbContext(DbContextOptions<ProvidersDbContext> options) : DbContext(options)
{
    public DbSet<ProviderType> Providerypes { get; set; } = default!;
    public DbSet<ProviderImplementation> ProviderImplementations { get; set; } = default!;
}


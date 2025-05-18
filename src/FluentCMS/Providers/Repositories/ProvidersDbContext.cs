using FluentCMS.Providers.Entities;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Providers.Repositories;

public class ProvidersDbContext
    (DbContextOptions<ProvidersDbContext> options) :
    DbContext(options),
    IAutoIdGeneratorDbContext,
    IAuditableEntityInterceptorDbContext,
    IEventPublisherDbContext
{

    public DbSet<ProviderType> Providerypes { get; set; } = default!;
    public DbSet<ProviderImplementation> ProviderImplementations { get; set; } = default!;
}

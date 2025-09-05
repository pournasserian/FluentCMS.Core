namespace FluentCMS.DataSeeder.EntityFramework;

public class EfSeedeingService<TContext>(IServiceProvider serviceProvider, SeedingOptions options, ILogger<SeedingService> logger) : ISeedingService where TContext : DbContext
{
    public Task<bool> CanSeed()
    {
        throw new NotImplementedException();
    }

    public Task EnsureSchema()
    {
        throw new NotImplementedException(); 
    }

    public Task ExecuteSeeding()
    {
        throw new NotImplementedException();
    }

    public Task SeedData()
    {
        throw new NotImplementedException();
    }
}

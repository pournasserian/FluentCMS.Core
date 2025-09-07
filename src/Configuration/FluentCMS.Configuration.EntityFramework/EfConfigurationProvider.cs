using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FluentCMS.Configuration.EntityFramework;

public class EfConfigurationProvider(DbContextOptions<ConfigurationDbContext> options, IConfigurationRoot configurationRoot) : ConfigurationProvider
{
    public override void Load()
    {
        var builder = new DbContextOptionsBuilder<ConfigurationDbContext>(options);
        using var dbContext =  new ConfigurationDbContext(builder.Options, configurationRoot);

        if (!dbContext.Database.CanConnect())
        {
            dbContext.Database.EnsureCreated();
        }
        else
        {
            try
            {
                var script = dbContext.Database.GenerateCreateScript();
                dbContext.Database.ExecuteSqlRaw(script);
            }
            catch (Exception)
            {
            }
        }

        if (dbContext is ConfigurationDbContext configDbContext)
        {
            configDbContext.Database.EnsureCreated();
            Data = configDbContext.Settings
                .ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}

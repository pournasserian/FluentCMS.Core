using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.Configuration.EntityFramework;

public static class ConfigurationServiceExtensions
{
    public static void AddEfConfiguration(this IHostApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContext<ConfigurationDbContext>((serviceProvider, options) =>
        {
            // OR use SQLite
            options.UseSqlite(connectionString);
        });

        builder.Configuration.Add<EfConfigurationSource>(configurationSource =>
        {
            configurationSource.Init(options =>
            {
                options.UseSqlite(connectionString);
            });
        });
    }
}

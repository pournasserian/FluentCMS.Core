using FluentCMS.Configuration.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.Configuration.Sqlite;

public static class ConfigurationServiceExtensions
{
    public static void AddEfConfiguration(this IHostApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContext<ConfigurationDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString);
        });

        builder.Configuration.Add<EfConfigurationSource>(configurationSource =>
        {
            configurationSource.Init(options =>
            {
                options.UseSqlServer(connectionString);
            });
        });
    }
}

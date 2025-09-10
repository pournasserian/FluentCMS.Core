using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.Configuration.SqlServer;

public static class SqlServerExtensions
{
    public static void AddSqlServerOptions(this IHostApplicationBuilder builder, string connectionString, long? reloadInterval = null)
    {
        DbConfigurationSource configSource = default!;

        builder.Configuration.Add<DbConfigurationSource>(source =>
        {
            configSource = source;

            source.Repository = new SqlServerOptionsRepository(connectionString);
            if (reloadInterval.HasValue)
                source.ReloadInterval = TimeSpan.FromSeconds(reloadInterval.Value);
        });

        builder.Services.AddSingleton(sp => configSource);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.DataAccess.EntityFramework.Sqlite;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteDataAccess<TContext>(this IServiceCollection services, string connectionString) where TContext : DbContext
    {
        //services.AddEntityFrameworkDataAccess<TContext>(options =>
        //{
        //    options.UseSqlite(connectionString);
        //});
        return services;
    }
}

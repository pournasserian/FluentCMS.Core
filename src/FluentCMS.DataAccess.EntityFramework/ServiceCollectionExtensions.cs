using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkDataAccess<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? optionsAction = default) where TContext : DbContext
    {
        services.AddDbContext<TContext>(options =>
        {
            optionsAction?.Invoke(options);
        });
     
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped(typeof(IEntityRepository<,>), typeof(EntityRepository<,>));
        services.AddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));

        services.AddScoped(typeof(IAuditableEntityRepository<,>), typeof(AuditableEntityRepository<,>));
        services.AddScoped(typeof(IAuditableEntityRepository<>), typeof(AuditableEntityRepository<>));

        return services;
    }
}

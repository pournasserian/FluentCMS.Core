using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.DataAccess.EntityFramework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkDataAccess<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? optionsAction = default) where TContext : DbContext
    {
        services.AddDbContext<TContext>(options =>
        {
            optionsAction?.Invoke(options);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();

        var registry = new RepositoryRegistry();
        services.AddSingleton(registry);

        foreach (var entityType in registry.CustomRepositoryTypes.Keys)
        {
            var interfaceType = registry.GetRepositoryInterfaceType(entityType);
            if (interfaceType != null)
            {
                var implementationType = registry.GetRepositoryImplementationType(interfaceType);
                if (implementationType != null)
                {
                    services.AddScoped(interfaceType, implementationType);
                }
            }
        }

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}

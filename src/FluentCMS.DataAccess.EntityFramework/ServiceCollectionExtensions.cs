using FluentCMS.Core.EventBus.Abstractions;
using FluentCMS.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.DataAccess.EntityFramework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkDataAccess<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? optionsAction = default) where TContext : DbContext
    {
        services.AddDbContext<TContext>((sp, options) =>
        {
            // Register the audit interceptor with the DbContext
            var eventPublisher = sp.GetService<IEventPublisher>();
            if (eventPublisher != null)
            {
                options.AddInterceptors(new EventBusSaveChangesInterceptor(eventPublisher));
            }
            
            // Configure query tracking behavior
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            // Apply any other options
            optionsAction?.Invoke(options);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

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
                else 
                {
                    // If no implementation type is found, register the interface with the default repository
                    services.AddScoped(interfaceType, sp => sp.GetRequiredService(typeof(IRepository<>).MakeGenericType(entityType)));
                }
            }
        }

        return services;
    }
}

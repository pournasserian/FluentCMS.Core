using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FluentCMS.DataAccess.EntityFramework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
    {
        services.AddKeyedSingleton("MainDbOptions", optionsAction);

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<EventBusInterceptor>();
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

    public static IServiceCollection AddCoreDbContext<TContext>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = default) where TContext : DbContext
    {
        // services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddUnitOfWorkByContextType(typeof(TContext));

        // Register the DbContext with the specified options
        services.AddDbContextByType(typeof(TContext), (sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<EventBusInterceptor>());
            // Configure query tracking behavior
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            // Apply any other options
            optionsAction?.Invoke(sp, options);
            var mainDbOptionsAction = sp.GetKeyedService<Action<IServiceProvider, DbContextOptionsBuilder>>("MainDbOptions");
            mainDbOptionsAction?.Invoke(sp, options);
        });

        return services;
    }

    private static IServiceCollection AddUnitOfWorkByContextType(this IServiceCollection services, Type contextType)
    {
        // Validate that the type is a DbContext
        if (!typeof(DbContext).IsAssignableFrom(contextType))
            throw new ArgumentException($"{contextType.Name} is not a DbContext type", nameof(contextType));

        // weh should create a generic type for UnitOfWork<TContext>
        var unitOfWorkType = typeof(UnitOfWork<>).MakeGenericType(contextType);

        // Register the UnitOfWork with the specified context type
        services.AddScoped(typeof(IUnitOfWork), unitOfWorkType);

        return services;
    }

    private static IServiceCollection AddDbContextByType(this IServiceCollection services, Type contextType, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
    {
        // Validate that the type is a DbContext
        if (!typeof(DbContext).IsAssignableFrom(contextType))
        {
            throw new ArgumentException($"{contextType.Name} is not a DbContext type", nameof(contextType));
        }

        // Get the generic method AddDbContext<>
        var addDbContextMethod = typeof(ServiceCollectionExtensions)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == nameof(AddEntityFrameworkDbContext) &&
                   m.IsGenericMethod &&
                   m.GetGenericArguments().Length == 1 &&
                   m.GetParameters().Length == 2);

        // Make it specific to our contextType
        var genericMethod = addDbContextMethod.MakeGenericMethod(contextType);

        // Invoke the method with our parameters
        genericMethod.Invoke(
            null,
            [
                services,
                optionsAction
            ]);

        return services;
    }

    private static IServiceCollection AddEntityFrameworkDbContext<TContext>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = default) where TContext : DbContext
    {
        services.AddDbContext<TContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<EventBusInterceptor>());

            // Configure query tracking behavior
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            // Apply any other options
            optionsAction?.Invoke(sp, options);
        });

        return services;
    }
}
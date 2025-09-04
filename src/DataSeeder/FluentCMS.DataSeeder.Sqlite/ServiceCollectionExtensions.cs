using FluentCMS.DataSeeder.Conditions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentCMS.DataSeeder.Sqlite;


/// <summary>
/// Extension methods for IServiceCollection to register database seeding services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds database seeding services to the service collection
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration for seeding options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDatabaseSeeding<TDbContext>(
        this IServiceCollection services,
        Action<SeedingOptions>? configureOptions = null)
        where TDbContext : DbContext
    {
        // Configure seeding options
        var options = new SeedingOptions();
        configureOptions?.Invoke(options);

        // Register options as singleton
        services.TryAddSingleton(options);

        // Register factory
        services.TryAddSingleton<SeedingServiceFactory>();

        // Register seeding service
        services.TryAddScoped(provider =>
        {
            var factory = provider.GetRequiredService<SeedingServiceFactory>();
            var context = provider.GetRequiredService<TDbContext>();
            return factory.CreateSeedingService(context);
        });

        // Auto-register all seeders in the calling assembly
        RegisterSeeders(services);

        return services;
    }

    /// <summary>
    /// Adds database seeding services with explicit connection string
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The database connection string</param>
    /// <param name="configureOptions">Optional configuration for seeding options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDatabaseSeeding(
        this IServiceCollection services,
        string connectionString,
        Action<SeedingOptions>? configureOptions = null)
    {
        // Configure seeding options
        var options = new SeedingOptions();
        configureOptions?.Invoke(options);

        // Register options as singleton
        services.TryAddSingleton(options);

        // Register factory
        services.TryAddSingleton<SeedingServiceFactory>();

        // Register seeding service
        services.TryAddScoped(provider =>
        {
            var factory = provider.GetRequiredService<SeedingServiceFactory>();
            return factory.CreateSeedingService(connectionString);
        });

        // Auto-register all seeders in the calling assembly
        RegisterSeeders(services);

        return services;
    }

    /// <summary>
    /// Adds a specific seeding condition
    /// </summary>
    /// <typeparam name="TCondition">The condition type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="condition">The condition instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSeedingCondition<TCondition>(
        this IServiceCollection services,
        TCondition condition)
        where TCondition : class, ISeedingCondition
    {
        services.Configure<SeedingOptions>(options =>
        {
            options.Conditions.Add(condition);
        });

        return services;
    }

    /// <summary>
    /// Adds a seeder implementation
    /// </summary>
    /// <typeparam name="TSeeder">The seeder type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class
    {
        services.TryAddTransient<TSeeder>();
        return services;
    }

    private static void RegisterSeeders(IServiceCollection services)
    {
        var callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
        var seederTypes = callingAssembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISeeder<>)))
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var seederType in seederTypes)
        {
            services.TryAddTransient(seederType);
        }
    }
}

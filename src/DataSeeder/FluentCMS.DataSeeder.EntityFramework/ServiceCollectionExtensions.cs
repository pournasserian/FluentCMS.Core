using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.DataSeeder.EntityFramework;

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
    public static IServiceCollection AddEfDataSeeding<TSeeder>(
        this IServiceCollection services,
        Action<SeedingOptions>? configureOptions = null)
        where TSeeder : ISeeder
    {
        // Configure seeding options
        var options = new SeedingOptions();
        configureOptions?.Invoke(options);



        return services;
    }
}

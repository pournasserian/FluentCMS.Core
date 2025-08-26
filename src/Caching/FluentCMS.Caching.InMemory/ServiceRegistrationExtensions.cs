using FluentCMS.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Caching.InMemory;

/// <summary>
/// Extension methods for setting up caching services in an IServiceCollection
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds in-memory caching services to the specified IServiceCollection
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to</param>
    /// <param name="setupAction">An Action to configure the MemoryCacheOptions</param>
    /// <returns>The IServiceCollection so that additional calls can be chained</returns>
    public static IServiceCollection AddInMemoryCaching(this IServiceCollection services, Action<MemoryCacheOptions>? setupAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add Microsoft's memory cache
        if (setupAction != null)
        {
            services.AddMemoryCache(setupAction);
        }
        else
        {
            services.AddMemoryCache();
        }

        // Register the cache provider
        services.AddScoped<ICacheProvider, InMemoryCacheProvider>();

        return services;
    }
}


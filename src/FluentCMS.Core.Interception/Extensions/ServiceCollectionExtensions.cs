using FluentCMS.Core.Interception.Abstractions;
using FluentCMS.Core.Interception.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentCMS.Core.Interception.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add interception capabilities.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a service with interception capabilities.
    /// </summary>
    /// <typeparam name="TService">The service interface.</typeparam>
    /// <typeparam name="TImplementation">The implementation type of the service.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddWithInterception<TService, TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TService : class
        where TImplementation : class, TService
    {
        // Register the implementation
        services.Add(new ServiceDescriptor(
            typeof(TImplementation), 
            typeof(TImplementation), 
            lifetime));
        
        // Register the proxied service
        services.Add(new ServiceDescriptor(
            typeof(TService),
            serviceProvider => {
                var implementation = serviceProvider.GetRequiredService<TImplementation>();
                var chain = serviceProvider.GetRequiredService<IInterceptorChain>();
                return ServiceProxy<TService>.Create(implementation, chain);
            },
            lifetime));
            
        return services;
    }
    
    /// <summary>
    /// Adds the interception framework to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInterceptionFramework(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IInterceptorChain, InterceptorChain>();
        
        return services;
    }
}

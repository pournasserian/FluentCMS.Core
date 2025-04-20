using FluentCMS.Core.Interception.Abstractions;
using FluentCMS.Core.Interception.Interceptors.HistoryTracking;
using FluentCMS.Core.Interception.Interceptors.HistoryTracking.LiteDB;
using FluentCMS.Core.Repositories.Abstractions;
using FluentCMS.Core.Repositories.LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentCMS.Core.Interception.Extensions;

/// <summary>
/// Extension methods for adding history tracking to the service collection.
/// </summary>
public static class HistoryTrackingExtensions
{
    /// <summary>
    /// Adds history tracking services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddHistoryTracking(this IServiceCollection services)
    {
        // Add the interception framework if it hasn't been added already
        services.AddInterceptionFramework();
        
        // Add the entity history interceptor
        services.TryAddScoped<IMethodInterceptor, EntityHistoryInterceptor>();
        
        // Add default user context accessor if none is registered
        services.TryAddScoped<IUserContextAccessor, DefaultUserContextAccessor>();
        
        return services;
    }
    
    /// <summary>
    /// Adds a LiteDB-based history recorder implementation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLiteDBHistoryRecorder(this IServiceCollection services)
    {
        services.TryAddScoped<IHistoryRecorder, LiteDBHistoryRecorder>();
        
        return services;
    }
    
    /// <summary>
    /// Adds a repository with history tracking.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRepositoryWithHistoryTracking<TEntity, TRepository>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TEntity : class, IBaseEntity
        where TRepository : class, IBaseEntityRepository<TEntity>
    {
        // Add history tracking if it hasn't been added already
        services.AddHistoryTracking();
        
        // Register the repository with interception
        services.AddWithInterception<IBaseEntityRepository<TEntity>, TRepository>(lifetime);
        
        return services;
    }
    
    /// <summary>
    /// Adds a LiteDB repository with history tracking.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLiteDBRepositoryWithHistoryTracking<TEntity>(
        this IServiceCollection services)
        where TEntity : class, IBaseEntity
    {
        // Add LiteDB history recorder if it hasn't been added already
        services.AddLiteDBHistoryRecorder();
        
        // Register the LiteDB repository with history tracking
        services.AddRepositoryWithHistoryTracking<TEntity, LiteDBRepository<TEntity>>();
        
        return services;
    }
}

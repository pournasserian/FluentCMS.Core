using FluentCMS.DataAccess.EntityFramework.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentCMS.DataAccess.EntityFramework;

/// <summary>
/// Extension methods for configuring database contexts and providers
/// </summary>
public static class DatabaseExtensions
{
    // Store both DbContext types and their local configurations
    private static readonly Dictionary<Type, Action<IServiceProvider, DbContextOptionsBuilder>> _pendingDbContextConfigs = [];

    // Flag to track if the database provider has been configured
    private static bool _databaseProviderConfigured = false;

    // Built-in interceptors to apply to all DbContexts
    private static readonly List<Type> _builtInInterceptorTypes = [typeof(AuditableEntityInterceptor), typeof(EventBusInterceptor)];

    /// <summary>
    /// Registers global interceptors to be applied to all DbContexts
    /// </summary>
    public static IServiceCollection AddGlobalInterceptors<TInterceptor>(this IServiceCollection services) where TInterceptor : class
    {
        // Register the interceptor in DI
        services.AddScoped<TInterceptor>();

        // Store the interceptor type
        var interceptorType = typeof(TInterceptor);
        if (!_builtInInterceptorTypes.Contains(interceptorType))
        {
            _builtInInterceptorTypes.Add(interceptorType);
        }

        return services;
    }

    /// <summary>
    /// Registers a DbContext type with local configurations for later provider setup.
    /// This should be called in each project layer that defines its own DbContext.
    /// </summary>
    public static IServiceCollection AddCoreDbContext<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder>? localConfigureOptions = null)
        where TContext : DbContext
    {
        var contextType = typeof(TContext);

        services.TryAddScoped<AuditableEntityInterceptor>();
        services.TryAddScoped<EventBusInterceptor>();

        // Create combined local configuration that includes built-in interceptors
        Action<IServiceProvider, DbContextOptionsBuilder> combinedLocalConfig = (sp, options) =>
        {
            // Apply built-in interceptors first
            ApplyBuiltInInterceptors(sp, options);

            // Then apply custom local configuration
            localConfigureOptions?.Invoke(sp, options);
        };

        if (_databaseProviderConfigured)
        {
            // If AddDatabase was already called, register immediately with the global provider config
            RegisterDbContext(services, contextType, combinedLocalConfig);
        }
        else
        {
            // Store the context type and its combined local configuration for later registration
            _pendingDbContextConfigs[contextType] = combinedLocalConfig;
        }

        return services;
    }

    /// <summary>
    /// Configures the database provider for all previously registered DbContexts.
    /// This should be called in the main application's Program.cs.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> globalConfigureOptions)
    {
        // Mark database provider as configured
        _databaseProviderConfigured = true;

        // Register all pending contexts with the global provider configuration
        foreach (var entry in _pendingDbContextConfigs)
        {
            var contextType = entry.Key;
            var localConfigureOptions = entry.Value;

            RegisterDbContext(services, contextType, localConfigureOptions, globalConfigureOptions);
        }

        // Clear the pending contexts as they're now registered
        _pendingDbContextConfigs.Clear();

        return services;
    }

    /// <summary>
    /// Helper method to register a DbContext with both local and global configurations
    /// </summary>
    private static void RegisterDbContext(
        IServiceCollection services,
        Type contextType,
        Action<IServiceProvider, DbContextOptionsBuilder>? localConfigureOptions,
        Action<IServiceProvider, DbContextOptionsBuilder>? globalConfigureOptions = null)
    {
        
        // Use reflection to call the generic AddDbContext method for this type 
        var addDbContextMethod = typeof(EntityFrameworkServiceCollectionExtensions)
            .GetMethods()
            .Single(
                m => m.IsPublic && 
                m.IsStatic && 
                m.IsGenericMethod &&
                m.GetGenericArguments().Length == 1 &&
                m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext) &&
                m.GetParameters().Length == 4 &&
                m.GetParameters()[0].ParameterType == typeof(IServiceCollection) &&
                m.GetParameters()[1].ParameterType == typeof(Action<IServiceProvider, DbContextOptionsBuilder>) &&
                m.GetParameters()[2].ParameterType == typeof(ServiceLifetime) &&
                m.GetParameters()[3].ParameterType == typeof(ServiceLifetime)).MakeGenericMethod(contextType);

        if (addDbContextMethod == null)
        {
            throw new InvalidOperationException(
                $"Could not find AddDbContext method for {contextType.Name}");
        }

        // Create a delegate that combines local and global configurations
        Action<IServiceProvider, DbContextOptionsBuilder> combinedConfigureOptions = (sp, options) =>
        {
            // Apply local configuration first (specific to this DbContext)
            localConfigureOptions?.Invoke(sp, options);

            // Then apply global configuration (database provider and common settings)
            globalConfigureOptions?.Invoke(sp, options);
        };

        // Create the proper generic delegate for this context type
        var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
        var serviceProviderType = typeof(IServiceProvider);
        var delegateType = typeof(Action<,>).MakeGenericType(serviceProviderType, optionsBuilderType);

        Action<IServiceProvider, DbContextOptionsBuilder> delegateAction = (sp, builder) => combinedConfigureOptions(sp, builder);

        addDbContextMethod.Invoke(null, [services, delegateAction, ServiceLifetime.Scoped, ServiceLifetime.Scoped]);
    }

    /// <summary>
    /// Applies built-in interceptors to the DbContext options
    /// </summary>
    private static void ApplyBuiltInInterceptors(IServiceProvider serviceProvider, DbContextOptionsBuilder options)
    {
        foreach (var interceptorType in _builtInInterceptorTypes)
        {
            // Get the interceptor instance from the service provider
            var interceptor = serviceProvider.GetService(interceptorType);

            if (interceptor != null)
            {
                // Check if it's a SaveChangesInterceptor
                if (interceptor is SaveChangesInterceptor saveChangesInterceptor)
                {
                    options.AddInterceptors(saveChangesInterceptor);
                }
                //// Check if it's a DbCommandInterceptor
                //else if (interceptor is DbCommandInterceptor commandInterceptor)
                //{
                //    options.AddInterceptors(commandInterceptor);
                //}
                //// Check if it's a ConnectionInterceptor
                //else if (interceptor is ConnectionInterceptor connectionInterceptor)
                //{
                //    options.AddInterceptors(connectionInterceptor);
                //}
                //// Check if it's a TransactionInterceptor
                //else if (interceptor is TransactionInterceptor transactionInterceptor)
                //{
                //    options.AddInterceptors(transactionInterceptor);
                //}
            }
        }
    }
}
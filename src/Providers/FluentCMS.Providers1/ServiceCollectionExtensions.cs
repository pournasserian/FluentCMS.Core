using FluentCMS.Providers.Abstractions;
using FluentCMS.Providers.Data;
using FluentCMS.Providers.DI;
using FluentCMS.Providers.Loading;
using FluentCMS.Providers.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers;

/// <summary>
/// Extension methods for setting up provider services in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds provider system services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configuration">The configuration instance to get provider options from.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddProviderSystem(this IServiceCollection services, IConfiguration configuration)
    {
        // Add provider system options
        services.Configure<ProviderSystemOptions>(configuration.GetSection("ProviderSystem"));
        
        // Add the provider DbContext
        services.AddDbContext<ProviderDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("ProviderDbConnection");
            options.UseSqlite(connectionString);
        });
        
        // Add provider system services
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddSingleton<ProviderAssemblyManager>();
        services.AddSingleton<DynamicServiceProvider>();
        services.AddSingleton<IProviderManager, ProviderManager>();
        
        return services;
    }

    /// <summary>
    /// Adds provider system services to the specified <see cref="IServiceCollection" /> with a custom database provider.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configuration">The configuration instance to get provider options from.</param>
    /// <param name="dbContextOptionsAction">An action to configure the DbContext options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddProviderSystem(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> dbContextOptionsAction)
    {
        // Add provider system options
        services.Configure<ProviderSystemOptions>(configuration.GetSection("ProviderSystem"));
        
        // Add the provider DbContext with custom options
        services.AddDbContext<ProviderDbContext>(dbContextOptionsAction);
        
        // Add provider system services
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddSingleton<ProviderAssemblyManager>();
        services.AddSingleton<DynamicServiceProvider>();
        services.AddSingleton<IProviderManager, ProviderManager>();
        
        return services;
    }
}

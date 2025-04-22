using FluentCMS.Core.Plugins.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FluentCMS.Core.Plugins;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluentCmsPlugins(
        this IServiceCollection services,
        string pluginDirectory = "plugins")
    {
        // Register plugin loader
        services.AddSingleton<IPluginLoader, PluginLoader>();
        
        // Register plugin manager
        services.AddSingleton<IPluginManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PluginManager>>();
            var pluginLoader = sp.GetRequiredService<IPluginLoader>();
            
            // If pluginDirectory is a relative path, make it absolute
            if (!Path.IsPathRooted(pluginDirectory))
            {
                var assemblyLocation = Assembly.GetEntryAssembly()?.Location
                    ?? Assembly.GetExecutingAssembly().Location;
                var baseDirectory = Path.GetDirectoryName(assemblyLocation) ?? Directory.GetCurrentDirectory();
                pluginDirectory = Path.Combine(baseDirectory, pluginDirectory);
            }
            
            return new PluginManager(logger, pluginLoader, pluginDirectory);
        });
        
        return services;
    }
}

public static class ApplicationBuilderExtensions
{
    public static async Task<IApplicationBuilder> UseFluentCmsPluginsAsync(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;
        var pluginManager = serviceProvider.GetRequiredService<IPluginManager>();
        
        // Initialize plugins
        await pluginManager.InitializeAsync(new ServiceCollection());
        
        // Start plugins
        await pluginManager.StartAllAsync();
        
        return app;
    }
    
    // Synchronous version that calls the async version
    public static IApplicationBuilder UseFluentCmsPlugins(this IApplicationBuilder app)
    {
        return app.UseFluentCmsPluginsAsync().GetAwaiter().GetResult();
    }
}

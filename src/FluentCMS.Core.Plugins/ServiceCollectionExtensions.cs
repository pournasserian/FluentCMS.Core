namespace FluentCMS.Core.Plugins;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlugins(this IServiceCollection services)
    {
        var pluginManager = new PluginManager();
        services.AddSingleton<IPluginManager>(pluginManager);
        return pluginManager.ConfigureServices(services);
    }

    public static IApplicationBuilder UsePlugins(this IApplicationBuilder app)
    {
        var pluginLoader = app.ApplicationServices.GetRequiredService<IPluginManager>();
        pluginLoader.Configure(app);
        return app;
    }
}
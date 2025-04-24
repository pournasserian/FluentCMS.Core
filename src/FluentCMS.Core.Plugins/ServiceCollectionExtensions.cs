namespace FluentCMS.Core.Plugins;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlugins(this IServiceCollection services)
    {
        var pluginLoader = new PluginLoader();
        services.AddSingleton<IPluginLoader>(pluginLoader);
        return pluginLoader.ConfigureServices(services);
    }

    public static IApplicationBuilder UsePlugins(this IApplicationBuilder app)
    {
        var pluginLoader = app.ApplicationServices.GetRequiredService<IPluginLoader>();
        pluginLoader.Configure(app);
        return app;
    }
}
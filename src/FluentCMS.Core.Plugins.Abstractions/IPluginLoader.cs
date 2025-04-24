namespace FluentCMS.Core.Plugins.Abstractions;

public interface IPluginLoader
{
    IServiceCollection ConfigureServices(IServiceCollection services);
    IApplicationBuilder Configure(IApplicationBuilder app);
    IEnumerable<IPlugin> GetPlugins();
    IEnumerable<IPluginMetadata> GetPluginMetadata();
}

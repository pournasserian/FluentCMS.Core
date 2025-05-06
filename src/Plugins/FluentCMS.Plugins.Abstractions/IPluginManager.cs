namespace FluentCMS.Plugins.Abstractions;

public interface IPluginManager
{
    void ConfigureServices(IHostApplicationBuilder builder);
    void Configure(IApplicationBuilder app);
    IEnumerable<IPlugin> GetPlugins();
    IEnumerable<IPluginMetadata> GetPluginMetadata();
}

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.Core.Plugins.Abstractions;

public interface IPluginInitializer
{
    void ConfigureServices(IHostApplicationBuilder builder);

    void Configure(IApplicationBuilder app);
}

public interface IPluginConfigurationManager
{
    IEnumerable<PluginMetadata> GetPlugins();
}

//public class PluginConfigurationManager : IPluginConfigurationManager
//{
//    public static readonly string ModulesFilename = "modules.json";

//    public IEnumerable<PluginMetadata> GetPlugins()
//    {
//    }
//}
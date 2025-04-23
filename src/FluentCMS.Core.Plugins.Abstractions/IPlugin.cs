using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Core.Plugins.Abstractions;

public interface IPlugin
{
    // Plugin metadata
    string Name { get; }
    string Version { get; }
    string Description { get; }
    bool IsEnabled { get; set; }
    
    // Plugin lifecycle methods
    bool Initialize(IServiceCollection services);
    Task<bool> Start();
    Task<bool> Stop();
}


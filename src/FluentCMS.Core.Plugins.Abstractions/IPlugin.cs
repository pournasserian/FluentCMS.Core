namespace FluentCMS.Core.Plugins.Abstractions;

public interface IPlugin
{
    IServiceCollection ConfigureServices(IServiceCollection services);
    IApplicationBuilder Configure(IApplicationBuilder app);
}
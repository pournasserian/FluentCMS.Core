namespace FluentCMS.Plugins.Abstractions;

public interface IPlugin
{
    void ConfigureServices(IHostApplicationBuilder builder);
    void Configure(IApplicationBuilder app);
}
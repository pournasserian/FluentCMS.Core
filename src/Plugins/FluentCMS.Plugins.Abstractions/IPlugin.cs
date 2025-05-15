namespace FluentCMS.Plugins.Abstractions;

public interface IPlugin
{
    int Order { get; }
    void Initialize(IApplicationBuilder app);
    void ConfigureServices(IHostApplicationBuilder builder);
    void Configure(IApplicationBuilder app);
}
using FluentCMS.Core.Plugins.Abstractions;
using FluentCMS.TodoApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.TodoApi;

public class TodoPlugin : IPlugin
{
    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ITodoService, TodoService>();
        return services;
    }

    public IApplicationBuilder Configure(IApplicationBuilder app)
    {
        return app;
    }
}

using FluentCMS.Core.Plugins.Abstractions;
using FluentCMS.TodoApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.TodoApi;

public class TodoPlugin : IPlugin
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ITodoService, TodoService>();
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}

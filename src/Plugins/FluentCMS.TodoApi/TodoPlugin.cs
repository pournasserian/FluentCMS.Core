using FluentCMS.Plugins.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using FluentCMS.TodoApi.Repositories;
using FluentCMS.TodoApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.TodoApi;

public class TodoPlugin : IPlugin
{
    public int Order => 1000;

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ITodoService, TodoService>();
        builder.Services.AddScoped<ITodoRepository, TodoRepository>();
        builder.Services.AddCoreDbContext<TodoDbContext>();
    }

    public void Configure(IApplicationBuilder app)
    {
        
    }

    public void Initialize(IApplicationBuilder app)
    {
        app.ApplicationServices.InitializeDbContext<TodoDbContext>().GetAwaiter().GetResult();
    }
}
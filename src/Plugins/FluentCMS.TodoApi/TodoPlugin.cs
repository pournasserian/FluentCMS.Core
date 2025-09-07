using FluentCMS.Providers.Plugins.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using FluentCMS.TodoApi.Models;
using FluentCMS.TodoApi.Repositories;
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
        builder.Services.AddGenericRepository<Todo, TodoDbContext>();
        builder.Services.AddEfDbContext<TodoDbContext>();
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
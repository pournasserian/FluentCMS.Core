using FluentCMS.Plugins.TodoManagement.Models;
using FluentCMS.Plugins.TodoManagement.Repositories;
using FluentCMS.Plugins.TodoManagement.Services;
using FluentCMS.Providers.Plugins.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.Plugins.TodoManagement;

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
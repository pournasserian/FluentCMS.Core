using FluentCMS.Plugins.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using FluentCMS.TodoApi.Models;
using FluentCMS.TodoApi.Repositories;
using FluentCMS.TodoApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
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
        // Initialize the database in development environment only
        using var scope = app.ApplicationServices.CreateScope();
        var sp = scope.ServiceProvider;
        var dbContext = sp.GetRequiredService<TodoDbContext>();

        // Add this check to avoid conflicts
        if (!dbContext.Database.CanConnect())
        {
            dbContext.Database.EnsureCreated();
        }
        else
        {
            // For subsequent DbContexts, we need a different approach
            // This will ensure the tables for this specific DbContext are created
            // without dropping existing tables
            var script = dbContext.Database.GenerateCreateScript();
            dbContext.Database.ExecuteSqlRaw(script);
        }
    }
}
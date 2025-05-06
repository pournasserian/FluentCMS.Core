using FluentCMS.Plugins.Abstractions;
using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework;
using FluentCMS.TodoApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.TodoApi;

public class TodoPlugin : IPlugin
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IApplicationExecutionContext, ApiExecutionContext>();
        builder.Services.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();
        builder.Services.AddScoped<ITodoService, TodoService>();
        builder.Services.AddScoped<ITodoRepository, TodoRepository>();
        builder.Services.AddCoreDbContext<ApplicationDbContext>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // Initialize the database in development environment only
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var dbContext = sp.GetRequiredService<ApplicationDbContext>();

            // This ensures the database is created based on the current model
            // Only use this when NOT using migrations
            dbContext.Database.EnsureCreated();

            // Optionally, you could seed initial data here
            // SeedData.Initialize(dbContext);
        }
    }
}
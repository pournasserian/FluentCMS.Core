using FluentCMS.Core.Plugins.Abstractions;
using FluentCMS.DataAccess.Abstractions;
using FluentCMS.DataAccess.EntityFramework;
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
        builder.Services.AddScoped<IApplicationExecutionContext, ApiExecutionContext>();
        builder.Services.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();
        builder.Services.AddScoped<ITodoService, TodoService>();
        builder.Services.AddScoped<ITodoRepository, TodoRepository>();
        //builder.Services.AddScoped<IUnitOfWork, UnitOfWork<ApplicationDbContext>>();
        builder.Services.AddCoreDbContext<ApplicationDbContext>();
    }

    public void Configure(IApplicationBuilder app)
    {

    }
}
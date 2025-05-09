using FluentCMS.Plugins;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using FluentCMS.Repositories.EntityFramework;
using FluentCMS.EventBus;
using FluentCMS.Api;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/myapp-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var connectionstring = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add plugin system
builder.AddPlugins(["FluentCMS"]);

builder.Services.AddDatabase((sp, options) =>
{
    options.UseSqlite(connectionstring);
});

// Add Serilog to the application
builder.Host.UseSerilog();

builder.Services.AddEventPublisher();

// Add services to the container.
builder.Services.AddFluentCmsApi();

var app = builder.Build();

app.UseFluentCmsApi();

// Use plugin system
app.UsePlugins();

try
{
    Log.Information("Starting web application");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

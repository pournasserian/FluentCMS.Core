using FluentCMS.Api;
using FluentCMS.EventBus;
using FluentCMS.Logging;
using FluentCMS.Plugins;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/myapp-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add plugin system
builder.AddPlugins(["FluentCMS"]);

builder.Services.AddSqliteDatabase(connectionString);

builder.Services.AddEventPublisher();

// Add services to the container.
builder.Services.AddFluentCmsApi();

var app = builder.Build();

StaticLoggerFactory.Initialize(app.Services.GetRequiredService<ILoggerFactory>());

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

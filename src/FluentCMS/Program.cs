using FluentCMS.Core.Api;
using FluentCMS.Core.EventBus;
using FluentCMS.Core.Plugins;
using FluentCMS.DataAccess.EntityFramework.Sqlite;
using FluentCMS.TodoApi;
using Serilog;
using Serilog.Events;

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

builder.Services.AddSqliteDataAccess<ApplicationDbContext>(connectionstring);

// Add Serilog to the application
builder.Host.UseSerilog();

builder.Services.AddEventBus();

// Add services to the container.
builder.Services.AddFluentCmsApi();

// Add plugin system
builder.AddPlugins();

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

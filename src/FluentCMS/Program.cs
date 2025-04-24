using FluentCMS.Core.Api;
using FluentCMS.Core.EventBus;
using FluentCMS.Core.Plugins;
using FluentCMS.Core.Repositories.LiteDB;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);


// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/myapp-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Add Serilog to the application
builder.Host.UseSerilog();

builder.Services.AddEventBus();

// Add services to the container.
builder.Services.AddFluentCmsApi();
//builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddLiteDBRepositories(builder.Configuration);

// Add plugin system
builder.Services.AddPlugins();

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

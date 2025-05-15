using FluentCMS.Api;
using FluentCMS.DynamicOptionsProvider;
using FluentCMS.EventBus;
using FluentCMS.Plugins;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

builder.AddDynamicConfiguration();
builder.Services.AddDynamicOptions<JwtOptions>(config => 
{
    config.Issuer = "test";
});

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

await app.InitializeDynamicOptions();
var x = app.Services.GetService<IOptions<JwtOptions>>();

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

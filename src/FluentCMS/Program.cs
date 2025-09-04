using FluentCMS.Api;
using FluentCMS.Configuration.EntityFramework;
using FluentCMS.Providers.Caching.InMemory;
using FluentCMS.Providers.EventBus.InMemory;
using FluentCMS.Providers.Plugins;
using FluentCMS.Repositories.Sqlite;
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

//builder.Services.AddDbContext<ConfigurationDbContext>((serviceProvider, options) =>
//{
//    // OR use SQLite
//    options.UseSqlite(connectionString);
//});

//builder.Configuration.Add<EfConfigurationSource>(configurationSource =>
//{
//    configurationSource.Init(options =>
//    {
//        options.UseSqlite(connectionString);
//    });
//});

builder.Host.UseSerilog();

// Add plugin system
builder.AddPlugins(["FluentCMS"]);

builder.Services.AddSqliteDatabase(connectionString);

builder.Services.AddEventPublisher();

builder.Services.AddInMemoryCaching();

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

using FluentCMS.Api;
using FluentCMS.Configuration;
using FluentCMS.DataSeeder;
using FluentCMS.DataSeeder.Sqlite;
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

var services = builder.Services;

builder.AddSqliteOptions(connectionString);

builder.Host.UseSerilog();

// Add plugin system
builder.AddPlugins(["FluentCMS"]);

// Set sqlite db to the repositories
services.AddSqliteDatabase(connectionString);

// Register providers
services.AddEventPublisher();
services.AddInMemoryCaching();

// Add services to the container.
services.AddFluentCmsApi();

// Data seeding dependencies
services.AddSqliteDatabaseManager(connectionString, options =>
{
    options.AssemblyPrefixesToScan.Add("FluentCMS");

    // Only create scheman and seed data in Development environment
    options.Conditions.Add(new EnvironmentCondition(
        builder.Environment,
        env => env.IsDevelopment()));
});

services.AddDbOptions<JwtOptions>(builder.Configuration, "JwtOptions");

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

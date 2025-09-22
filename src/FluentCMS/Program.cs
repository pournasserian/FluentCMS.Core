using FluentCMS.Api;
using FluentCMS.Configuration.Sqlite;
using FluentCMS.Database.Abstractions;
using FluentCMS.Database.Extensions;
using FluentCMS.Database.Sqlite;
using FluentCMS.DataSeeding;
using FluentCMS.DataSeeding.Conditions;
using FluentCMS.Plugins.TodoManager;
using FluentCMS.Providers;
using FluentCMS.Providers.EventBus.InMemory;
using FluentCMS.Providers.Plugins;
using FluentCMS.Providers.Repositories.EntityFramework;
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

services.AddDatabaseManager(options =>
{
    // Default database for general services
    options.SetDefault().UseSqlite(connectionString);
});

builder.Host.UseSerilog();

services.AddProviders(options =>
    {
        options.AssemblyPrefixesToScan.Add("FluentCMS");
        options.IgnoreExceptions = false; // Set to true to ignore exceptions during provider loading
    }).UseEntityFramework();

// Add plugin system
builder.AddPlugins(["FluentCMS"]);

// Set sqlite db to the repositories
services.AddSqliteDatabase(connectionString);

// Register providers
services.AddEventPublisher();

// Add services to the container.
services.AddFluentCmsApi();

//// Configure seeding options
//services.AddDataSeeders(options =>
//{
//    options.IgnoreExceptions = false; // Fail fast on errors
//    options.Timeout = TimeSpan.FromMinutes(10); // Custom timeout

//    // Add conditions
//    options.Conditions.Add(new EnvironmentCondition(builder.Environment, e => e.IsDevelopment()));
//});

//services.AddSchemaValidators(options =>
//{
//    options.IgnoreExceptions = false;

//    // Only run schema validation in Development
//    options.Conditions.Add(new EnvironmentCondition(builder.Environment, e => e.IsDevelopment()));
//});

var app = builder.Build();

app.UseFluentCmsApi();


using (var scope = app.Services.CreateScope())
{
    var servicesProvider = scope.ServiceProvider;
    try
    {
        // Run schema validators
        var dbmanagers = servicesProvider.GetService<IDatabaseManager>();
        var x = servicesProvider.GetService<IDatabaseManager<ITodoDatabaseMarker>>();
        var k = servicesProvider.GetService<IDatabaseManager<IDefaultLibraryMarker>>();
        var t = await dbmanagers.DatabaseExists();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred during schema validation or data seeding");
        throw; // Rethrow to stop application startup
    }
}

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

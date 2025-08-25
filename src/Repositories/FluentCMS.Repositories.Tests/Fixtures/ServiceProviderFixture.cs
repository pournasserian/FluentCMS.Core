using FluentCMS.Logging;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Repositories.Tests.Fixtures;

public class ServiceProviderFixture
{
    public IServiceProvider ServiceProvider { get; private set; }

    public ServiceProviderFixture()
    {

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        var services = new ServiceCollection();

        services.AddScoped<IApplicationExecutionContext, SystemExecutionContext>();

        services.AddEventPublisher();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Add SQLite in-memory database
        services.AddSqliteDatabase("DataSource=:memory:");

        // Add repository for TestEntity
        services.AddGenericRepository<TestEntity, TestDbContext>();

        //// Add DbContext
        //services.AddDbContext<TestDbContext>(options =>
        //{
        //    options.UseSqlite("DataSource=:memory:");
        //});

        ServiceProvider = services.BuildServiceProvider();

        // Ensure database is created
        using var scope = ServiceProvider.CreateScope();
        StaticLoggerFactory.Initialize(scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();
    }
}

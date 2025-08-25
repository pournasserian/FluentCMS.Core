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
        services.AddSqliteDatabase("Data Source=:memory:");

        // Add repository for TestEntity
        services.AddGenericRepository<TestEntity, TestDbContext>();

        ServiceProvider = services.BuildServiceProvider();

        //var repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        //var entity = new TestEntity
        //{
        //    Name = "Test Entity",
        //    Description = "Test Description",
        //    Value = 42
        //};
        //var addedEntity = repository.Add(entity).Result;
        //dbContext.SaveChanges();
    }

    public IServiceScope CreateScope()
    {
        // Ensure database is created
        var serviceScope = ServiceProvider.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();
        dbContext.SaveChanges();
        return serviceScope;
    }

}

namespace FluentCMS.Plugins.AuditTrailManager;

public class AuditTrailManagerPlugin : IPlugin
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddDbContext<AuditTrailDbContext>((provider, options) =>
        {
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            var dbConfig = provider.GetRequiredService<IDatabaseConfiguration>();
            dbConfig.ConfigureDbContext(options);
        });

        services.AddTransient<IEventSubscriber<RepositoryEntityCreatedEvent>, AuditTrailHandler>();
        services.AddTransient<IEventSubscriber<RepositoryEntityUpdatedEvent>, AuditTrailHandler>();
        services.AddTransient<IEventSubscriber<RepositoryEntityRemovedEvent>, AuditTrailHandler>();

        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IAuditTrailRepository, AuditTrailRepository>();
        services.AddAutoMapper(typeof(MappingProfile));
    }

    public void Configure(IApplicationBuilder app)
    {
        // Initialize the database in development environment only
        using var scope = app.ApplicationServices.CreateScope();
        var sp = scope.ServiceProvider;
        var dbContext = sp.GetRequiredService<AuditTrailDbContext>();

        // Add this check to avoid conflicts
        if (!dbContext.Database.CanConnect())
        {
            dbContext.Database.EnsureCreated();
        }
        else
        {
            // For subsequent DbContexts, we need a different approach
            // This will ensure the tables for this specific DbContext are created
            // without dropping existing tables
            var script = dbContext.Database.GenerateCreateScript();
            dbContext.Database.ExecuteSqlRaw(script);
        }
    }
}

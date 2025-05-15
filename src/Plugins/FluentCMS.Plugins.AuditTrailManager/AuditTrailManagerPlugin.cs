namespace FluentCMS.Plugins.AuditTrailManager;

public class AuditTrailManagerPlugin : IPlugin
{
    public int Order => 0;

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddCoreDbContext<AuditTrailDbContext>();
        services.AddTransient<IEventSubscriber<RepositoryEntityCreatedEvent>, AuditTrailHandler>();
        services.AddTransient<IEventSubscriber<RepositoryEntityUpdatedEvent>, AuditTrailHandler>();
        services.AddTransient<IEventSubscriber<RepositoryEntityRemovedEvent>, AuditTrailHandler>();

        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IAuditTrailRepository, AuditTrailRepository>();
        services.AddAutoMapper(typeof(MappingProfile));
    }

    public void Configure(IApplicationBuilder app)
    {
    }

    public void Initialize(IApplicationBuilder app)
    {
        app.ApplicationServices.InitializeDbContext<AuditTrailDbContext>().GetAwaiter().GetResult();
    }
}

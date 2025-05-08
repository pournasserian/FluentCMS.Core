using FluentCMS.Plugins.AuditTrailManager.Repositories;
using FluentCMS.Plugins.AuditTrailManager.Services;

namespace FluentCMS.Plugins.AuditTrailManager;

public class AuditTrailManagerPlugin : IPlugin
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddCoreDbContext<AuditTrailDbContext>();
        services.AddTransient(typeof(IEventSubscriber<>), typeof(AuditTrailHandler<>));
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IAuditTrailRepository, AuditTrailRepository>();
        services.AddAutoMapper(typeof(MappingProfile));
    }

    public void Configure(IApplicationBuilder app)
    { 
        // Initialize the database in development environment only
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var dbContext = sp.GetRequiredService<AuditTrailDbContext>();

            // This ensures the database is created based on the current model
            // Only use this when NOT using migrations
            dbContext.Database.EnsureCreated();

            // Optionally, you could seed initial data here
            // SeedData.Initialize(dbContext);
        }
    }
}

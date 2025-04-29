namespace FluentCMS.Core.Plugins.History;

public class RepositoryHistoryPlugin : IPlugin
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        services.TryAddTransient(typeof(IEventSubscriber<>), typeof(EntityHistoryEventHandler<>));
        services.TryAddScoped<IEntityHistoryService, EntityHistoryService>();
        services.TryAddScoped<IEntityHistoryRepository, EntityHistoryRepository>();
        services.AddAutoMapper(typeof(MappingProfile));
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentCMS.Core.Plugins.History;

public class RepositoryHistoryPlugin : IPlugin
{
    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.TryAddTransient(typeof(IEventSubscriber<>), typeof(EntityHistoryEventHandler<>));
        return services;
    }

    public IApplicationBuilder Configure(IApplicationBuilder app)
    {
        return app;
    }    
}

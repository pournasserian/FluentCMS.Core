using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.Core.Plugins.History;

public class RepositoryHistoryPlugin : IPlugin
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services.TryAddTransient(typeof(IEventSubscriber<>), typeof(EntityHistoryEventHandler<>));
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}

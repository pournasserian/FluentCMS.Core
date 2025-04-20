using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Core.EventBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        // Register core event system
        services.AddScoped<IEventPublisher, EventPublisher>();
        
        return services;
    }
}

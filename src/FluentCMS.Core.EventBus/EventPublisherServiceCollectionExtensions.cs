namespace FluentCMS.Core.EventBus;

public static class EventPublisherServiceCollectionExtensions
{
    /// <summary>
    /// Adds the event publisher system to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventPublisher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the generic event publisher
        services.TryAddScoped<IEventPublisher, EventPublisher>();

        return services;
    }

    public static IServiceCollection AddEventSubscriber<TEvent, TSubscriber>(this IServiceCollection services)
        where TEvent : class, IEvent
        where TSubscriber : class, IEventSubscriber<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IEventSubscriber<TEvent>, TSubscriber>();

        return services;
    }
}

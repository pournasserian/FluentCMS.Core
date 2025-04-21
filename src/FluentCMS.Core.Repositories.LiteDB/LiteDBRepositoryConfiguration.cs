using FluentCMS.Core.Repositories.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using FluentCMS.Core.EventBus;

namespace FluentCMS.Core.Repositories.LiteDB;

public static class LiteDBRepositoryConfiguration
{
    public static IServiceCollection AddLiteDBRepositories(this IServiceCollection services, LiteDBOptions options)
    {

        services.AddTransient(typeof(IEventSubscriber<>), typeof(EntityHistoryEventHandler<>));
        services.AddScoped<IEventPublisher, EventPublisher>();

        services.AddScoped(typeof(IEntityHistoryRepository<>), typeof(EntityHistoryRepository<>));

        // Register the options
        services.TryAddSingleton(Options.Create(options));

        // Register the LiteDB context as a singleton
        services.TryAddSingleton<ILiteDBContext>(provider =>
        {
            var resolvedOptions = provider.GetRequiredService<IOptions<LiteDBOptions>>().Value;
            return new LiteDBContext(resolvedOptions);
        });

        // Register the generic repository
        services.TryAddScoped(typeof(IBaseEntityRepository<>), typeof(LiteDBRepository<>));

        return services;
    }

    public static IServiceCollection AddLiteDBRepositories(this IServiceCollection services, Action<LiteDBOptions> configureOptions)
    {
        var options = new LiteDBOptions();
        configureOptions(options);

        return AddLiteDBRepositories(services, options);
    }

    public static IServiceCollection AddLiteDBRepositories(this IServiceCollection services, IConfiguration configuration, string sectionName = "LiteDB")
    {
        services.AddTransient(typeof(IEventSubscriber<>), typeof(EntityHistoryEventHandler<>));

        services.AddScoped<IEventPublisher, EventPublisher>();

        services.AddScoped(typeof(IEntityHistoryRepository<>), typeof(EntityHistoryRepository<>));

        // Configure options from settings
        services.Configure<LiteDBOptions>(configuration.GetSection(sectionName));

        // Register the LiteDB context as a singleton
        services.TryAddSingleton<ILiteDBContext>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<LiteDBOptions>>().Value;
            return new LiteDBContext(options);
        });

        // Register the generic repository
        services.TryAddScoped(typeof(IBaseEntityRepository<>), typeof(LiteDBRepository<>));

        return services;
    }
}
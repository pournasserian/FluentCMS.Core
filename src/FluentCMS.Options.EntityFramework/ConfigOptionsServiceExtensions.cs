using Microsoft.Extensions.Options;

namespace FluentCMS.Options.EntityFramework;

public static class ConfigOptionsServiceExtensions
{
    private static readonly Dictionary<Type, object> _pendingConfigurations = [];

    // Register options with the registry and set up for change notification
    public static IServiceCollection AddDbOptions<TOptions>(this IServiceCollection services, Action<TOptions>? action = default) where TOptions : class, new()
    {
        var optionInstance = new TOptions();
        action?.Invoke(optionInstance);
        var type = typeof(TOptions);

        // Queue the configuration action to be applied when the registry is created
        if (!_pendingConfigurations.ContainsKey(type))
        {
            _pendingConfigurations[type] = optionInstance;
        }

        return services;
    }

    public static void AddDbConfigOptions(this IServiceCollection services)
    {
        services.AddCoreDbContext<ConfigOptionsDbContext>();
        services.AddTransient(typeof(IConfigureOptions<>), typeof(EntityFrameworkOptionsProvider<>));
        services.AddSingleton(sp =>
        {
            return new ConfigOptionsRegistery(sp, _pendingConfigurations);
        });
    }
}

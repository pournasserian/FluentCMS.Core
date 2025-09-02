using FluentCMS.Settings.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentCMS.Settings;

public static class SettingsServiceExtensions
{
    public static IServiceCollection AddSettings(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsChangeNotifier, SettingsChangeNotifier>();
        services.AddMemoryCache();
        
        services.AddScoped<ISettingsService>(sp =>
            new SettingsService(
                sp.GetRequiredService<ISettingsRepository>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ISettingsChangeNotifier>()));

        return services;
    }

    public static IServiceCollection BindSetting<T>(this IServiceCollection services, string key) where T : class, new()
    {
        services.AddSingleton<IConfigureOptions<T>>(sp =>
            new SettingsOptionsSource<T>(
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<ISettingsChangeNotifier>(),
                key));
        
        services.AddSingleton<IOptionsChangeTokenSource<T>>(sp => (SettingsOptionsSource<T>)sp.GetRequiredService<IConfigureOptions<T>>());

        return services;
    }
}

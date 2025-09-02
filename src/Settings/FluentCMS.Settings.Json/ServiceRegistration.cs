using FluentCMS.Settings.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Settings.Json;

public static class ServiceRegistration
{
    public static IServiceCollection UseJsonSettings(this IServiceCollection services, string path)
    {
        services.AddSettings();

        services.AddSingleton<ISettingsRepository>(_ => new JsonFileSettingsRepository(path));

        // Optional: hook file changes to app change tokens (live reload for all keys)
        var sp = services.BuildServiceProvider();

        if (sp.GetService<ISettingsChangeNotifier>() is { } notifier &&
            sp.GetService<ISettingsRepository>() is JsonFileSettingsRepository jf)
            jf.ExternalChanged += notifier.SignalAll;

        return services;
    }
}

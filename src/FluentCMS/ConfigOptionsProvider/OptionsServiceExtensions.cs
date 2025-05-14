using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentCMS.ConfigOptionsProvider;

public static class OptionsServiceExtensions
{
    public static IServiceCollection AddOptions<TOptions>(
        this IServiceCollection services,
        Action<TOptions> configureDefaults = null)
        where TOptions : class, new()
    {
        // Add regular options with defaults
        services.AddOptions<TOptions>()
            .Configure(options => configureDefaults?.Invoke(options));

        // Register the custom provider
        services.AddSingleton<IConfigureOptions<TOptions>, OptionsProvider<TOptions>>();

        return services;
    }
}

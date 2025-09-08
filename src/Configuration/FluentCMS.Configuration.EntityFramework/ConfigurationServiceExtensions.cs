using FluentCMS.Configuration.EntityFramework;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Configuration;

public static class ConfigurationServiceExtensions
{
    public static IServiceCollection AddConfigurationSevices(this IServiceCollection services)
    {
        services.AddScoped<ISettingRepository, SettingRepository>();

        services.AddScoped<ISettingService, SettingService>();

        return services;
    }
}

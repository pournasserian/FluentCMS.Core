using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace FluentCMS.Configuration;

public static class ServiceCollectionExtensions
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static IServiceCollection AddDbOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, string sectionName, bool seedData = true) where TOptions : class, new()
    {
        services.Configure<ProviderCatalogOptions>(options =>
        {
            if (seedData)
            {
                var regType = typeof(TOptions);
                var bound = configuration.GetSection(sectionName).Get<TOptions>();
                var registration = new OptionRegistration
                {
                    Section = sectionName,
                    Type = typeof(TOptions),
                    DefaultValue = JsonSerializer.Serialize(bound, regType, jsonSerializerOptions)
                };
                options.Types.Add(registration);
            }
        });

        services.TryAddSingleton<IOptionsCatalog, OptionsCatalog>();

        services.AddOptions<TOptions>()
            .Bind(configuration.GetRequiredSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register the hosted service to seed the database at startup
        // Avoid multiple registration for multiple calls of AddDbOptions
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, OptionsDbSeeder>());
        services.AddHostedService<OptionsDbSeeder>();

        return services;
    }
}

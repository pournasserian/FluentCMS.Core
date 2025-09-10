using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentCMS.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Module developers call this to declare their options and bind to configuration.
    /// The section will be persisted as a single JSON row in SQLite.
    /// </summary>
    public static IServiceCollection AddDbOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, string sectionName) where TOptions : class, new()
    {
        services.Configure<ProviderCatalogOptions>(options =>
        {
            options.Types.Add(new OptionRegistration { Section = sectionName, Type = typeof(TOptions) });
        });

        services.TryAddSingleton<IOptionsCatalog, OptionsCatalog>();

        services.AddOptions<TOptions>()
            .Bind(configuration.GetRequiredSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static void AddSqliteOptions(this IHostApplicationBuilder builder, string connectionString, long? reloadInterval = null, bool enableDataSeeding = true)
    {
        SqliteConfigurationSource configSource = default!;

        builder.Configuration.Add<SqliteConfigurationSource>(source =>
        {
            source.ConnectionString = connectionString;
            configSource = source;
            if (reloadInterval.HasValue)
                source.ReloadInterval = TimeSpan.FromSeconds(reloadInterval.Value);
        });

        builder.Services.AddSingleton(sp => configSource);

        if (enableDataSeeding)
            builder.Services.AddHostedService<OptionsDbSeeder>();
    }
}

using FluentCMS.Options.Repositories;
using FluentCMS.Options.Services;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FluentCMS.Options;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDbOptionsServices(this IServiceCollection services)
    {
        services.TryAddScoped<IOptionsRepository, OptionsRepository>();
        services.TryAddScoped<IOptionsService, OptionsService>();
        services.AddEfDbContext<OptionsDbContext>();

        return services;
    }

    public static OptionsBuilder<T> AddDbOptions<T>(this IServiceCollection services, string alias, string configSection) where T : class, new()
    {
        services.AddDbOptionsServices();

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var value = new T();
            config.GetSection(configSection).Bind(value);

            return new OptionsDescriptor(
                Alias: alias,
                ConfigSection: configSection,
                Type: typeof(T),
                DefaultValue: value
            );
        });


        return services.AddOptions<T>()
                    .BindConfiguration(configSection)
                    .PostConfigure<IServiceProvider>((opt, sp) =>
                    {
                        using var scope = sp.CreateScope();
                        var service = scope.ServiceProvider.GetRequiredService<IOptionsService>();
                        service.Bind(alias, opt).GetAwaiter().GetResult();
                    });
    }

    public static OptionsBuilder<T> AddDbOptions<T>(this IServiceCollection services, string alias) where T : class, new()
    {
        services.AddSingleton(new OptionsDescriptor(
            Alias: alias,
            ConfigSection: null,
            Type: typeof(T),
            DefaultValue: new T()
        ));

        return services.AddOptions<T>()
                    .PostConfigure<IServiceProvider>((opt, sp) =>
                    {
                        using var scope = sp.CreateScope();
                        var service = scope.ServiceProvider.GetRequiredService<IOptionsService>();
                        service.Bind(alias, opt).GetAwaiter().GetResult();
                    });
    }
}

using FluentCMS.Providers.Repositories.Abstractions;
using FluentCMS.Repositories.EntityFramework;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Providers.Repositories.EntityFramework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkProviderRepository(this IServiceCollection services)
    {
        services.AddEfDbContext<ProviderDbContext>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Plugins.Authentication.Stores;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultStores(this IServiceCollection services)
    {
        services.AddScoped<IUserStore<User>, UserStore>();
        services.AddScoped<IRoleStore<Role>, RoleStore>();
        return services;
    }
}

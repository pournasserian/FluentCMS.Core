using FluentCMS.Core.Repositories.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Core.Repositories.LiteDB;

public static class LiteDBRepositoryConfiguration
{
    public static IServiceCollection AddLiteDBRepositories(this IServiceCollection services, string connectionString)
    {
        // Register the LiteDB context as a singleton
        services.AddSingleton<ILiteDBContext>(provider => new LiteDBContext(connectionString));

        // Register the generic repository
        services.AddScoped(typeof(IBaseEntityRepository<>), typeof(LiteDBRepository<>));

        return services;
    }
}
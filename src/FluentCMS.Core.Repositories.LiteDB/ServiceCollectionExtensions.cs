namespace FluentCMS.Core.Repositories.LiteDB;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiteDBRepositories(this IServiceCollection services, IConfiguration configuration, string sectionName = "LiteDB")
    {
        // Configure options from settings
        services.Configure<LiteDBOptions>(configuration.GetSection(sectionName));

        // Register the LiteDB context as a singleton
        services.TryAddSingleton<ILiteDBContext>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<LiteDBOptions>>().Value;
            return new LiteDBContext(options);
        });

        // Register the generic repository
        services.TryAddScoped(typeof(IAuditableEntityRepository<>), typeof(AuditableEntityRepository<>));
        services.TryAddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));

        return services;
    }
}
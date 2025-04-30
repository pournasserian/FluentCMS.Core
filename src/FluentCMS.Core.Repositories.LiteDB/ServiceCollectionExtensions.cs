namespace FluentCMS.Core.Repositories.LiteDB;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiteDBRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        // Validate LiteDBOptions after binding
        services.AddOptions<LiteDBOptions>()
            .Bind(configuration.GetSection(nameof(LiteDBOptions)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register the LiteDB context as a singleton
        services.TryAddSingleton<ILiteDBContext, LiteDBContext>(); ;

        // Register the generic repository
        services.TryAddScoped(typeof(IAuditableEntityRepository<>), typeof(AuditableEntityRepository<>));
        services.TryAddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));

        return services;
    }
}
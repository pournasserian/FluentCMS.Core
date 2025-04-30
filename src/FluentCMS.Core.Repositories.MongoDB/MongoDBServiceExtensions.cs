namespace FluentCMS.Core.Repositories.MongoDB;

public static class MongoDbServiceExtensions
{
    public static IServiceCollection AddMongoDbRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        // Validate MongoDBOptions after binding
        services.AddOptions<MongoDBOptions>()
            .Bind(configuration.GetSection(nameof(MongoDBOptions)))
            .ValidateDataAnnotations();

        // Register the MongoDB context as a singleton
        services.AddSingleton<IMongoDBContext, MongoDBContext>();

        // register default GUID serializer for MongoDB
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        // Register the generic repository
        services.TryAddScoped(typeof(IAuditableEntityRepository<>), typeof(AuditableEntityRepository<>));
        services.TryAddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));

        return services;
    }
}

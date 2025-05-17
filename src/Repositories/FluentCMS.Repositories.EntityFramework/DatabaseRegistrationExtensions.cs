namespace FluentCMS.Repositories.EntityFramework;

public static class DatabaseRegistrationExtensions
{
    // Distinct, explicit name instead of overriding the standard method
    public static IServiceCollection AddGlobalDbContext<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? additionalConfiguration = null) where TContext : DbContext
    {
        return services.AddDbContext<TContext>((provider, options) =>
        {
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            var interceptors = provider.GetServices<IInterceptor>().ToList();
            foreach (var interceptor in interceptors)
            {
                options.AddInterceptors(interceptor);
            }

            // Apply global configuration first
            var dbConfig = provider.GetRequiredService<IDatabaseConfiguration>();
            dbConfig.ConfigureDbContext(options);

            // Then apply context-specific configuration if provided
            additionalConfiguration?.Invoke(options);
        });
    }

    public static IServiceCollection AddSqliteDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<IRepositortyEventPublisher, RepositortyEventPublisher>();

        services.AddScoped<IInterceptor, IdGeneratorInterceptor>();
        services.AddScoped<IInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<IInterceptor, RepositoryEventBusPublisherInterceptor>();

        services.AddSingleton<IDatabaseConfiguration>(sp =>
        {
            return new SqliteDatabaseConfiguration(connectionString);
        });
        return services;
    }
}

namespace FluentCMS.Repositories.EntityFramework;

public static class DatabaseRegistrationExtensions
{
    public static IServiceCollection AddGenericRepository<TEntity, TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? additionalConfiguration = null) where TEntity : class, IEntity where TContext : DbContext
    {
        services.AddEfDbContext<TContext>(additionalConfiguration);
        services.AddScoped<IRepository<TEntity>, Repository<TEntity, TContext>>();
        services.AddScoped<ITransactionalRepository<TEntity>, Repository<TEntity, TContext>>();
        return services;
    }

    public static IServiceCollection AddEfDbContext<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? additionalConfiguration = null) where TContext : DbContext
    {
        services.AddScoped<IRepositoryEventPublisher, RepositoryEventPublisher>();

        // This should be first, interceptors orders are important
        services.AddScoped<IInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<IInterceptor, RepositoryEventBusPublisherInterceptor>();

        services.AddDbContext<TContext>((provider, options) =>
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

        return services;
    }

    public static IServiceCollection AddSqliteDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IDatabaseConfiguration>(sp =>
        {
            return new SqliteDatabaseConfiguration(connectionString);
        });
        return services;
    }

    public static IServiceCollection AddSqlServerDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IDatabaseConfiguration>(sp =>
        {
            return new SqlServerDatabaseConfiguration(connectionString);
        });
        return services;
    }
}

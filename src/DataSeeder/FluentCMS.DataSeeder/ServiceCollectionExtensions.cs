namespace FluentCMS.DataSeeder;

public static class ServiceCollectionExtensions
{
    /// Call this ONCE in the main app (Program.cs) to enable runtime, blocking seeding
    public static IServiceCollection AddDataSeeding(this IServiceCollection services, Action<SeedingOptions>? configure = null)
    {
        // Configure options
        var opts = new SeedingOptions();
        configure?.Invoke(opts);
        services.AddSingleton(opts);

        // TODO: Replace with real logger
        var discoveryLogger = NullLoggerFactory.Instance.CreateLogger<SeedingDiscovery>();
        var discovery = new SeedingDiscovery(opts, discoveryLogger);
        var seeders = discovery.GetSeerders();
        foreach (var seederType in seeders)
        {
            services.AddTransient(typeof(ISeeder), seederType);
        }

        services.AddScoped<SeedingService>();

        services.AddHostedService<SeedingHostedService>();

        return services;
    }
}
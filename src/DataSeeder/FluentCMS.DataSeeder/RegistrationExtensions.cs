namespace FluentCMS.DataSeeder;

public static class RegistrationExtensions
{
    /// Call this ONCE in the main app (Program.cs) to enable runtime, blocking seeding
    public static IHostApplicationBuilder AddDataSeeding(this IHostApplicationBuilder builder, Action<SeedingOptions>? configure = null)
    {
        // Configure options
        var opts = new SeedingOptions();
        configure?.Invoke(opts);
        builder.Services.AddSingleton(opts);

        // TODO: Replace with real logger
        var discoveryLogger = NullLoggerFactory.Instance.CreateLogger<SeedingDiscovery>();
        var discovery = new SeedingDiscovery(opts, discoveryLogger);
        var seeders = discovery.GetSeerders();
        foreach (var seederType in seeders)
        {
            builder.Services.AddTransient(typeof(ISeeder), seederType);
        }
        
        builder.Services.AddTransient<SeedingService>();

        builder.Services.AddSingleton<SeedingHostedService>();
        builder.Services.AddHostedService<SeedingHostedService>();

        return builder;
    }
}
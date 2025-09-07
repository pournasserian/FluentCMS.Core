namespace FluentCMS.DataSeeder;

internal sealed class SeedingHostedService(IServiceProvider serviceProvider, ILogger<SeedingHostedService> logger, SeedingOptions seedingOptions) : IHostedService
{
    private readonly ILogger<SeedingHostedService>? _logger = seedingOptions.EnableLogging ? logger : null;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<SeedingService>();
        try
        {
            _logger?.LogInformation("Starting database schema creation if needed...");
            await seedingService.EnsureSchema(cancellationToken);
            _logger?.LogInformation("Database schema creation process completed.");
            _logger?.LogInformation("Starting data seeding process...");
            await seedingService.EnsureSeedData(cancellationToken);
            _logger?.LogInformation("Data seeding process completed.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An error occurred during the seeding process.");
            if (!seedingOptions.IgnoreExceptions)
                throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

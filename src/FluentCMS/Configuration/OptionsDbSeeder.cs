namespace FluentCMS.Configuration;

public sealed class OptionsDbSeeder(ILogger<OptionsDbSeeder> logger, IOptionsCatalog registrations, DbConfigurationSource source, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await source.Repository.EnsureCreated(cancellationToken);

            foreach (var reg in registrations.All)
            {
                var rows = await source.Repository.Upsert(reg, cancellationToken);
                if (rows > 0)
                {
                    logger.LogInformation("Seeded options for {Section}", reg.Section);
                }
            }

            // After seeding, refresh the configuration provider so IOptions binds from DB
            source.Provider.TriggerReload();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed seeding options DB");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

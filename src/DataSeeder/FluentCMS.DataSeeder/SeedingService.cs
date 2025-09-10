namespace FluentCMS.DataSeeder;

internal class SeedingService(IEnumerable<ISeeder> seeders, ILogger<SeedingService> logger, SeedingOptions seedingOptions)
{
    private readonly ILogger<SeedingService>? _logger = seedingOptions.EnableLogging ? logger : null;
    private readonly IEnumerable<ISeeder> seeders = seeders.OrderBy(s => s.Order);

    public async Task EnsureSchema(CancellationToken cancellationToken)
    {
        foreach (var seedingCondition in seedingOptions.Conditions)
        {
            if (!await seedingCondition.ShouldSeed())
            {
                _logger?.LogInformation("Seeding condition '{ConditionName}' not met. Skipping seeding process.", seedingCondition.Name);
                return;
            }
        }

        foreach (var seeder in seeders)
        {
            _logger?.LogInformation("Processing seeder '{SeederName}' with order {Order}.", seeder.GetType().Name, seeder.Order);
            try
            {
                if (await seeder.ShouldCreateSchema(cancellationToken))
                {
                    await CreateSchema(seeder, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error occurred while creating schema for seeder '{SeederName}'.", seeder.GetType().Name);

                if (!seedingOptions.IgnoreExceptions)
                    throw;
            }
        }
    }

    public async Task EnsureSeedData(CancellationToken cancellationToken)
    {
        foreach (var seedingCondition in seedingOptions.Conditions)
        {
            if (!await seedingCondition.ShouldSeed())
            {
                _logger?.LogInformation("Seeding condition '{ConditionName}' not met. Skipping seeding process.", seedingCondition.Name);
                return;
            }
        }

        foreach (var seeder in seeders)
        {
            if (await seeder.ShouldSeed(cancellationToken))
            {
                try
                {
                    await SeedData(seeder, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error occurred while seeding data for seeder '{SeederName}'.", seeder.GetType().Name);

                    if (!seedingOptions.IgnoreExceptions)
                        throw;
                }
            }
        }
    }

    private async Task CreateSchema(ISeeder seeder, CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Creating schema for seeder '{SeederName}'.", seeder.GetType().Name);
        await seeder.CreateSchema(cancellationToken);
        _logger?.LogInformation("Schema created for seeder '{SeederName}'.", seeder.GetType().Name);
    }

    private async Task SeedData(ISeeder seeder, CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Seeding data for seeder '{SeederName}'.", seeder.GetType().Name);
        await seeder.SeedData(cancellationToken);
        _logger?.LogInformation("Data seeded for seeder '{SeederName}'.", seeder.GetType().Name);
    }
}
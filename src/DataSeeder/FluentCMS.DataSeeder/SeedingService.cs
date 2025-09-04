using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FluentCMS.DataSeeder;

/// <summary>
/// Main service for orchestrating the database seeding process
/// </summary>
public class SeedingService(IServiceProvider serviceProvider, SeedingOptions options, ILogger<SeedingService> logger) : ISeedingService
{
    protected readonly ILogger<SeedingService>? Logger = options.EnableLogging ? logger : null;

    public async Task ExecuteSeeding(DbContext context)
    {
        Logger?.LogInformation("Starting database seeding process");

        try
        {
            // Check conditions
            if (!await CanSeed(context))
            {
                Logger?.LogInformation("Seeding conditions not met, skipping seeding");
                return;
            }

            // Ensure schema exists
            if (options.EnsureSchemaCreated)
            {
                await EnsureSchema(context);
            }

            // Seed data
            await SeedData(context);

            Logger?.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error occurred during database seeding");
            throw new SeedingException("Failed to execute seeding process", ex);
        }
    }

    public async Task<bool> CanSeed(DbContext context)
    {
        if (options.Conditions.Count == 0)
            return true;

        try
        {
            foreach (var condition in options.Conditions)
            {
                var result = await condition.ShouldSeed(context);

                Logger?.LogDebug("Condition '{ConditionName}' result: {Result}", condition.Name, result);

                if (!result)
                    return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error checking seeding conditions");
            return false;
        }
    }

    public virtual async Task EnsureSchema(DbContext context)
    {
        try
        {
            Logger?.LogInformation("Ensuring database schema exists");

            await context.Database.EnsureCreatedAsync();

            Logger?.LogInformation("Database schema ensured");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error ensuring database schema");
            throw new SeedingException("Failed to ensure database schema", ex);
        }
    }

    public async Task SeedData(DbContext context)
    {
        try
        {
            Logger?.LogInformation("Starting data seeding");

            var seeders = DiscoverSeeders();
            var orderedSeeders = seeders.OrderBy(s => s.Order).ToList();

            Logger?.LogInformation("Found {SeederCount} seeders", orderedSeeders.Count);

            if (options.UseTransaction)
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    await ExecuteSeeders(orderedSeeders, context);
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                await ExecuteSeeders(orderedSeeders, context);
            }

            Logger?.LogInformation("Data seeding completed");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error during data seeding");
            throw new SeedingException("Failed to seed data", ex);
        }
    }

    private async Task ExecuteSeeders(List<SeederInfo> seeders, DbContext context)
    {
        foreach (var seederInfo in seeders)
        {
            var attempts = 0;
            while (attempts < options.MaxRetryAttempts)
            {
                try
                {
                    Logger?.LogDebug("Executing seeder: {SeederType} (Order: {Order})", seederInfo.SeederType.Name, seederInfo.Order);

                    var seeder = serviceProvider.GetService(seederInfo.SeederType);
                    if (seeder != null)
                    {
                        var seedMethod = seederInfo.SeederType.GetMethod("Seed");
                        await (Task)seedMethod!.Invoke(seeder, [context])!;
                    }

                    break; // Success, exit retry loop
                }
                catch (Exception ex)
                {
                    attempts++;
                    if (attempts >= options.MaxRetryAttempts)
                    {
                        Logger?.LogError(ex, "Failed to execute seeder {SeederType} after {Attempts} attempts", seederInfo.SeederType.Name, attempts);
                        throw;
                    }

                    Logger?.LogWarning(ex, "Seeder {SeederType} failed, attempt {Attempt}/{MaxAttempts}", seederInfo.SeederType.Name, attempts, options.MaxRetryAttempts);

                    await Task.Delay(options.RetryDelayMs);
                }
            }
        }
    }

    private List<SeederInfo> DiscoverSeeders()
    {
        var seeders = new List<SeederInfo>();
        var assemblies = GetAssembliesToScan();

        foreach (var assembly in assemblies)
        {
            var seederTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISeeder<>)))
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (var seederType in seederTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(seederType);
                    if (instance != null)
                    {
                        var orderProperty = seederType.GetProperty("Order");
                        var order = (int)(orderProperty?.GetValue(instance) ?? 0);
                        seeders.Add(new SeederInfo(seederType, order));
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "Failed to create instance of seeder {SeederType}", seederType.Name);
                }
            }
        }

        return seeders;
    }

    private List<Assembly> GetAssembliesToScan()
    {
        var assemblies = new List<Assembly>();

        if (options.AssembliesToScan.Count != 0)
        {
            foreach (var assemblyName in options.AssembliesToScan)
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(assemblyName));
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "Failed to load assembly {AssemblyName}", assemblyName);
                }
            }
        }
        else
        {
            // Default to scanning the calling assembly
            assemblies.Add(Assembly.GetCallingAssembly());
        }

        return assemblies;
    }

    private record SeederInfo(Type SeederType, int Order);
}

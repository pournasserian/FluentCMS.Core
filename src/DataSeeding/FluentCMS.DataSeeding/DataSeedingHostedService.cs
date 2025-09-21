using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeding;

/// <summary>
/// Hosted service responsible for performing database schema validation and data seeding operations
/// during application startup. This service ensures the database is properly initialized before
/// the application begins normal operation.
/// </summary>
/// <param name="serviceProvider">Service provider for creating scoped dependencies</param>
/// <param name="logger">Logger instance for tracking seeding operations and errors</param>
internal sealed class DataSeedingHostedService(IServiceProvider serviceProvider, ILogger<DataSeedingHostedService> logger) : IHostedService
{
    /// <summary>
    /// Starts the data seeding process, including schema validation and seed data insertion.
    /// This method is called automatically when the host starts.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task representing the asynchronous seeding operation</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Create a new service scope to resolve dependencies with proper lifetime management
        using var scope = serviceProvider.CreateScope();
        
        // Resolve required services from the scoped service provider
        var schemaValidatorService = scope.ServiceProvider.GetRequiredService<SchemaValidatorService>();
        var dataSeederService = scope.ServiceProvider.GetRequiredService<DataSeederService>();
        
        try
        {
            // Step 1: Ensure database schema exists and is up to date
            logger.LogInformation("Starting database schema creation if needed...");
            await schemaValidatorService.EnsureSchema(cancellationToken);
            logger.LogInformation("Database schema creation process completed.");
            
            // Step 2: Seed initial data into the database
            logger.LogInformation("Starting data seeding process...");
            await dataSeederService.EnsureSeedData(cancellationToken);
            logger.LogInformation("Data seeding process completed.");
        }
        catch (Exception ex)
        {
            // Log the error and re-throw to prevent application startup with incomplete seeding
            logger.LogError(ex, "An error occurred during the seeding process.");
            throw;
        }
    }

    /// <summary>
    /// Stops the data seeding service. Since seeding is a one-time startup operation,
    /// no cleanup is required during shutdown.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A completed task</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // No cleanup required for data seeding operations
        return Task.CompletedTask;
    }
}

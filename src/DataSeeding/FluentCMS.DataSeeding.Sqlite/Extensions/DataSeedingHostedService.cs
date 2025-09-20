using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Sqlite.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeding.Sqlite.Extensions;

/// <summary>
/// Background service that executes database seeding during application startup.
/// Runs once during the application lifecycle after all services are configured.
/// </summary>
public class DataSeedingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSeedingHostedService> _logger;
    private readonly SqliteDataSeedingOptions _options;

    /// <summary>
    /// Initializes a new instance of DataSeedingHostedService.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    /// <param name="logger">Logger for seeding operations</param>
    /// <param name="options">SQLite data seeding options</param>
    public DataSeedingHostedService(
        IServiceProvider serviceProvider,
        ILogger<DataSeedingHostedService> logger,
        SqliteDataSeedingOptions options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Executes the seeding workflow in the background.
    /// This method runs once during application startup.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for graceful shutdown</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogDebug("DataSeedingHostedService starting execution");

            // Validate configuration before proceeding
            var validationErrors = _options.Validate();
            if (validationErrors.Any())
            {
                _logger.LogError("Configuration validation failed: {Errors}", 
                    string.Join("; ", validationErrors));
                return;
            }

            // Create a scope for the seeding operation
            using var scope = _serviceProvider.CreateScope();
            var scopedServiceProvider = scope.ServiceProvider;

            // Get the seeding engine
            var seedingEngine = new SqliteDataSeedingEngine(
                _options, 
                scopedServiceProvider, 
                scopedServiceProvider.GetRequiredService<ILogger<SqliteDataSeedingEngine>>());

            // Execute seeding
            var result = await seedingEngine.ExecuteSeeding(stoppingToken);

            // Log final results
            if (result.IsSuccess)
            {
                _logger.LogInformation("Database seeding completed successfully in {Duration}ms. " +
                    "Schemas: {SchemaCount} ({SchemasCreated} created), " +
                    "Seeders: {SeederCount} ({DataSeeded} executed), " +
                    "Conditions: {ConditionCount} ({ConditionsPassed})",
                    result.Duration.TotalMilliseconds,
                    result.TotalSchemaValidators, result.SchemasCreated,
                    result.TotalDataSeeders, result.DataSeeded,
                    result.ConditionResults.Count(), result.AllConditionsPassed ? "passed" : "failed");
            }
            else
            {
                _logger.LogError(result.Exception, "Database seeding failed after {Duration}ms",
                    result.Duration.TotalMilliseconds);
            }

            _logger.LogDebug("DataSeedingHostedService completed execution");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Database seeding was cancelled during application shutdown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in DataSeedingHostedService: {ErrorMessage}", ex.Message);
            
            // Don't rethrow - we don't want seeding failures to prevent application startup
            // unless specifically configured to do so
            if (!_options.IgnoreExceptions)
            {
                // If we're not ignoring exceptions, we should fail fast
                // This will prevent the application from starting if seeding fails
                throw;
            }
        }
    }

    /// <summary>
    /// Called when the service is requested to stop.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("DataSeedingHostedService stopping");
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Called when the service is being disposed.
    /// </summary>
    public override void Dispose()
    {
        _logger.LogTrace("DataSeedingHostedService disposing");
        base.Dispose();
    }
}

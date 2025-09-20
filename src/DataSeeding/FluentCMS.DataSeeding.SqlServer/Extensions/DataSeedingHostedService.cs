using System;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.SqlServer.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeding.SqlServer.Extensions;

/// <summary>
/// Hosted service that executes SQL Server data seeding operations at application startup.
/// This service runs automatically when the application starts and executes all configured
/// schema validators and data seeders according to their priority and conditions.
/// </summary>
public class DataSeedingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSeedingHostedService> _logger;
    private readonly SqlServerDataSeedingOptions _options;

    /// <summary>
    /// Initializes a new instance of DataSeedingHostedService.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped services</param>
    /// <param name="logger">Logger for hosted service operations</param>
    /// <param name="options">SQL Server data seeding options</param>
    public DataSeedingHostedService(
        IServiceProvider serviceProvider,
        ILogger<DataSeedingHostedService> logger,
        SqlServerDataSeedingOptions options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Executes the data seeding workflow as a background service.
    /// This method is called automatically when the application starts.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for stopping the service</param>
    /// <returns>Task representing the async operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("SQL Server data seeding hosted service starting");

            // Wait a short moment to allow other services to initialize
            await Task.Delay(100, stoppingToken);

            // Create a new scope for the seeding operation
            using var scope = _serviceProvider.CreateScope();
            var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<SqlServerDataSeedingEngine>>();

            // Create the seeding engine with scoped services
            var engine = new SqlServerDataSeedingEngine(_options, scope.ServiceProvider, scopedLogger);

            // Execute seeding and log results
            var result = await engine.ExecuteSeeding(stoppingToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("SQL Server data seeding completed successfully. " +
                    "Duration: {Duration}, Conditions Evaluated: {ConditionCount}, " +
                    "Schemas Processed: {SchemaCount}, Data Operations: {DataCount}",
                    result.Duration,
                    result.ConditionResults.Count,
                    result.SchemaResults.Count,
                    result.DataResults.Count);

                // Log configuration summary for debugging
                var configSummary = _options.GetConfigurationSummary();
                _logger.LogDebug("Seeding configuration: {@ConfigSummary}", configSummary);
            }
            else
            {
                var errorMessage = result.Exception?.Message ?? "Unknown error";
                _logger.LogError(result.Exception, "SQL Server data seeding failed: {ErrorMessage}", errorMessage);

                // In production scenarios, we might want to stop the application on seeding failure
                // For now, we'll log the error and continue
                if (!_options.IgnoreExceptions)
                {
                    _logger.LogCritical("Data seeding failed and IgnoreExceptions is false. " +
                        "Consider reviewing seeding configuration and data.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("SQL Server data seeding was cancelled during application shutdown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SQL Server data seeding hosted service: {ErrorMessage}", ex.Message);
            
            // In some scenarios, we might want to stop the application entirely
            // if critical seeding operations fail. For now, we'll log and continue.
            if (!_options.IgnoreExceptions)
            {
                _logger.LogCritical("Critical error in data seeding. Application may be in an inconsistent state.");
            }
        }
    }

    /// <summary>
    /// Called when the application host is ready to start the service.
    /// This can be used for additional initialization if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("SQL Server data seeding hosted service is starting");
        
        // Log configuration summary at startup
        try
        {
            var configSummary = _options.GetConfigurationSummary();
            _logger.LogDebug("Starting with configuration: {@ConfigSummary}", configSummary);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not log configuration summary: {ErrorMessage}", ex.Message);
        }

        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Called when the application host is performing a graceful shutdown.
    /// This ensures any ongoing seeding operations are properly cancelled.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("SQL Server data seeding hosted service is stopping");
        await base.StopAsync(cancellationToken);
        _logger.LogDebug("SQL Server data seeding hosted service stopped");
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Abstractions;
using FluentCMS.DataSeeding.Engine;
using FluentCMS.DataSeeding.Models;
using FluentCMS.DataSeeding.Sqlite.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeding.Sqlite.Engine;

/// <summary>
/// SQLite-specific implementation of the data seeding engine.
/// Orchestrates the complete seeding workflow including condition evaluation,
/// schema validation, and data seeding operations.
/// </summary>
public class SqliteDataSeedingEngine
{
    private readonly SqliteDataSeedingOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SqliteDataSeedingEngine> _logger;
    private readonly AssemblyScanner _assemblyScanner;
    private readonly DependencyResolver _dependencyResolver;

    /// <summary>
    /// Initializes a new instance of SqliteDataSeedingEngine.
    /// </summary>
    /// <param name="options">SQLite data seeding options</param>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    /// <param name="logger">Logger for seeding operations</param>
    public SqliteDataSeedingEngine(
        SqliteDataSeedingOptions options,
        IServiceProvider serviceProvider,
        ILogger<SqliteDataSeedingEngine> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _assemblyScanner = new AssemblyScanner();
        _dependencyResolver = new DependencyResolver();
    }

    /// <summary>
    /// Executes the complete seeding workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Complete seeding result with timing and status information</returns>
    public async Task<SeedingResult> ExecuteSeeding(CancellationToken cancellationToken = default)
    {
        var result = new SeedingResult
        {
            StartedAt = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting SQLite database seeding with connection: {ConnectionString}", 
                MaskConnectionString(_options.ConnectionString));

            // Create seeding context
            using var context = new SqliteSeedingContext(_options.ConnectionString, _serviceProvider);

            // Step 1: Evaluate conditions
            _logger.LogDebug("Evaluating {ConditionCount} seeding conditions", _options.Conditions.Count);
            var conditionEvaluation = await _dependencyResolver.ExecuteConditions(
                _options.Conditions, context, cancellationToken);
            var allConditionsPassed = conditionEvaluation.AllPassed;
            var conditionResults = conditionEvaluation.Results;

            result.ConditionResults.AddRange(conditionResults);

            if (!allConditionsPassed)
            {
                _logger.LogInformation("Seeding conditions not met. Skipping database seeding.");
                result.IsSuccess = true; // Not an error - conditions intentionally prevented seeding
                return result;
            }

            _logger.LogInformation("All seeding conditions passed. Proceeding with database seeding.");

            // Step 2: Discover and validate components
            var (validators, seeders) = await DiscoverComponents(cancellationToken);
            
            // Validate priorities
            var priorityIssues = _dependencyResolver.ValidatePriorities(validators, seeders);
            if (priorityIssues.Any())
            {
                foreach (var issue in priorityIssues)
                {
                    _logger.LogWarning("Priority configuration issue: {Issue}", issue);
                }
            }

            _logger.LogInformation("Discovered {ValidatorCount} schema validators and {SeederCount} data seeders",
                validators.Count(), seeders.Count());

            // Step 3: Execute schema validators
            _logger.LogDebug("Executing schema validation and creation");
            var schemaResults = await _dependencyResolver.ExecuteSchemaValidators(
                validators, context, _options.IgnoreExceptions, cancellationToken);

            result.SchemaResults.AddRange(schemaResults);

            // Step 4: Execute data seeders
            _logger.LogDebug("Executing data seeding operations");
            var dataResults = await _dependencyResolver.ExecuteDataSeeders(
                seeders, context, _options.IgnoreExceptions, cancellationToken);

            result.DataResults.AddRange(dataResults);

            result.IsSuccess = true;

            _logger.LogInformation("Database seeding completed successfully. " +
                "Schemas created: {SchemasCreated}, Data seeded: {DataSeeded}, Duration: {Duration}ms",
                result.SchemasCreated, result.DataSeeded, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            result.IsSuccess = false;

            _logger.LogError(ex, "Database seeding failed after {Duration}ms: {ErrorMessage}",
                stopwatch.ElapsedMilliseconds, ex.Message);

            if (!_options.IgnoreExceptions)
            {
                throw;
            }
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Discovers schema validators and data seeders from configured assemblies.
    /// </summary>
    private async Task<(IEnumerable<ISchemaValidator> Validators, IEnumerable<IDataSeeder> Seeders)> 
        DiscoverComponents(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Scanning assemblies with patterns: {Patterns}", 
            string.Join(", ", _options.AssemblySearchPatterns));

        // Discover types
        var validatorTypes = await _assemblyScanner.ScanForTypes<ISchemaValidator>(
            _options.AssemblySearchPatterns, cancellationToken);
        var seederTypes = await _assemblyScanner.ScanForTypes<IDataSeeder>(
            _options.AssemblySearchPatterns, cancellationToken);

        _logger.LogDebug("Found {ValidatorTypeCount} validator types and {SeederTypeCount} seeder types",
            validatorTypes.Count(), seederTypes.Count());

        // Create instances using DI
        var validators = new List<ISchemaValidator>();
        var seeders = new List<IDataSeeder>();

        foreach (var validatorType in validatorTypes)
        {
            try
            {
                var validator = (ISchemaValidator)ActivatorUtilities.CreateInstance(_serviceProvider, validatorType);
                validators.Add(validator);
                _logger.LogTrace("Created validator: {ValidatorType} (Priority: {Priority})", 
                    validatorType.Name, validator.Priority);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create validator instance: {ValidatorType}", validatorType.Name);
                if (!_options.IgnoreExceptions)
                {
                    throw;
                }
            }
        }

        foreach (var seederType in seederTypes)
        {
            try
            {
                var seeder = (IDataSeeder)ActivatorUtilities.CreateInstance(_serviceProvider, seederType);
                seeders.Add(seeder);
                _logger.LogTrace("Created seeder: {SeederType} (Priority: {Priority})", 
                    seederType.Name, seeder.Priority);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create seeder instance: {SeederType}", seederType.Name);
                if (!_options.IgnoreExceptions)
                {
                    throw;
                }
            }
        }

        return (validators, seeders);
    }

    /// <summary>
    /// Masks sensitive information in connection strings for logging.
    /// </summary>
    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "[empty]";

        // For SQLite, connection strings typically contain file paths
        // We'll show only the filename for security
        if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            var startIndex = connectionString.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase);
            var afterDataSource = connectionString.Substring(startIndex + "Data Source=".Length);
            var endIndex = afterDataSource.IndexOf(';');
            var dataSource = endIndex >= 0 ? afterDataSource.Substring(0, endIndex) : afterDataSource;
            
            // Extract filename only
            var fileName = System.IO.Path.GetFileName(dataSource.Trim());
            return $"Data Source={fileName}***";
        }

        return "[connection string masked]";
    }

    /// <summary>
    /// Gets seeding statistics for monitoring and debugging.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Dictionary of seeding statistics</returns>
    public async Task<Dictionary<string, object>> GetSeedingStatistics(CancellationToken cancellationToken = default)
    {
        var stats = new Dictionary<string, object>();

        try
        {
            using var context = new SqliteSeedingContext(_options.ConnectionString, _serviceProvider);
            
            // Get basic database info
            var tableCount = await context.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table'", 
                cancellationToken);
            
            stats.Add("TotalTables", Convert.ToInt32(tableCount));
            stats.Add("ConnectionString", MaskConnectionString(_options.ConnectionString));
            stats.Add("AssemblyPatterns", _options.AssemblySearchPatterns);
            stats.Add("ConditionCount", _options.Conditions.Count);
            stats.Add("IgnoreExceptions", _options.IgnoreExceptions);
            stats.Add("CachedAssemblyPatterns", _assemblyScanner.CachedPatternCount);
        }
        catch (Exception ex)
        {
            stats.Add("Error", ex.Message);
        }

        return stats;
    }
}

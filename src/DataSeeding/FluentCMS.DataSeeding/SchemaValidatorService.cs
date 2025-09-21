using FluentCMS.DataSeeding.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentCMS.DataSeeding;

/// <summary>
/// Service responsible for validating and creating database schemas using registered schema validators.
/// Executes validators in priority order and ensures all conditions are met before proceeding.
/// </summary>
internal class SchemaValidatorService(IEnumerable<ISchemaValidator> schemaValidators, ILogger<SchemaValidatorService> logger, IOptions<SchemaValidatorOptions> schemaValidatorOptions)
{
    // Order schema validators by priority (lower numbers execute first)
    private readonly IEnumerable<ISchemaValidator> schemaValidators = schemaValidators.OrderBy(s => s.Priority);

    /// <summary>
    /// Ensures database schemas are valid and creates them if necessary.
    /// All configured conditions must be met before schema validation begins.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    public async Task EnsureSchema(CancellationToken cancellationToken)
    {
        // Check all conditions - ALL must be met to proceed
        foreach (var condition in schemaValidatorOptions.Value.Conditions)
        {
            if (!await condition.ShouldExecute(cancellationToken))
            {
                logger.LogInformation("Schema validation condition '{Name}' not met. Skipping schema creation process.", condition.Name);
                return;
            }
        }

        // Validate and create schemas for each validator in priority order
        foreach (var validator in schemaValidators)
        {
            if (await validator.ValidateSchema(cancellationToken))
            {
                logger.LogInformation("Schema for '{ValidatorName}' is valid. Skipping creation.", validator.GetType().Name);
                continue;
            }
            else
            {
                // Schema validation failed, attempt to create the schema
                await CreateSchema(validator, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Creates schema using the specified validator with error handling and logging.
    /// </summary>
    /// <param name="schemaValidator">The validator to use for schema creation</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    private async Task CreateSchema(ISchemaValidator schemaValidator, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Creating schema for '{Name}'.", schemaValidator.GetType().Name);
            await schemaValidator.CreateSchema(cancellationToken);
            logger.LogInformation("Schema created for '{Name}'.", schemaValidator.GetType().Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating schema for validator '{ValidatorName}'.", schemaValidator.GetType().Name);
            
            // Re-throw exception unless configured to ignore exceptions
            if (!schemaValidatorOptions.Value.IgnoreExceptions)
                throw;
        }
    }
}

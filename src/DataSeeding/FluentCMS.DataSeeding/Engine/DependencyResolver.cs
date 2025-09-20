using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Abstractions;
using FluentCMS.DataSeeding.Models;

namespace FluentCMS.DataSeeding.Engine;

/// <summary>
/// Resolves dependencies and orders components by priority for execution.
/// Ensures schema validators run before data seeders and within each group, components execute in priority order.
/// </summary>
public class DependencyResolver
{
    /// <summary>
    /// Orders schema validators by priority (lower numbers execute first).
    /// </summary>
    /// <param name="validators">Collection of schema validator instances</param>
    /// <returns>Schema validators ordered by priority</returns>
    public IEnumerable<ISchemaValidator> OrderSchemaValidators(IEnumerable<ISchemaValidator> validators)
    {
        return validators.OrderBy(v => v.Priority).ThenBy(v => v.GetType().FullName);
    }

    /// <summary>
    /// Orders data seeders by priority (lower numbers execute first).
    /// </summary>
    /// <param name="seeders">Collection of data seeder instances</param>
    /// <returns>Data seeders ordered by priority</returns>
    public IEnumerable<IDataSeeder> OrderDataSeeders(IEnumerable<IDataSeeder> seeders)
    {
        return seeders.OrderBy(s => s.Priority).ThenBy(s => s.GetType().FullName);
    }

    /// <summary>
    /// Executes schema validators in priority order, with optional error handling.
    /// </summary>
    /// <param name="validators">Collection of schema validator instances</param>
    /// <param name="context">The seeding context</param>
    /// <param name="ignoreExceptions">Whether to continue execution if a validator fails</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of schema validation results</returns>
    public async Task<IEnumerable<SchemaValidationResult>> ExecuteSchemaValidators(
        IEnumerable<ISchemaValidator> validators,
        SeedingContext context,
        bool ignoreExceptions = false,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SchemaValidationResult>();
        var orderedValidators = OrderSchemaValidators(validators);

        foreach (var validator in orderedValidators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new SchemaValidationResult
            {
                ValidatorType = validator.GetType().FullName ?? validator.GetType().Name,
                Priority = validator.Priority
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // Check if schema is already valid
                result.SchemaWasValid = await validator.ValidateSchema(context, cancellationToken);

                // Create schema if needed
                if (!result.SchemaWasValid)
                {
                    await validator.CreateSchema(context, cancellationToken);
                    result.SchemaCreated = true;
                }

                result.Duration = DateTime.UtcNow - startTime;
                results.Add(result);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Duration = DateTime.UtcNow - startTime;
                results.Add(result);

                if (!ignoreExceptions)
                {
                    // Stop execution on first failure
                    throw;
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Executes data seeders in priority order, with optional error handling.
    /// </summary>
    /// <param name="seeders">Collection of data seeder instances</param>
    /// <param name="context">The seeding context</param>
    /// <param name="ignoreExceptions">Whether to continue execution if a seeder fails</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of data seeding results</returns>
    public async Task<IEnumerable<DataSeedingResult>> ExecuteDataSeeders(
        IEnumerable<IDataSeeder> seeders,
        SeedingContext context,
        bool ignoreExceptions = false,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DataSeedingResult>();
        var orderedSeeders = OrderDataSeeders(seeders);

        foreach (var seeder in orderedSeeders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new DataSeedingResult
            {
                SeederType = seeder.GetType().FullName ?? seeder.GetType().Name,
                Priority = seeder.Priority
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // Check if data already exists
                result.DataAlreadyExists = await seeder.HasData(context, cancellationToken);

                // Seed data if needed
                if (!result.DataAlreadyExists)
                {
                    await seeder.SeedData(context, cancellationToken);
                    result.DataSeeded = true;
                }

                result.Duration = DateTime.UtcNow - startTime;
                results.Add(result);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Duration = DateTime.UtcNow - startTime;
                results.Add(result);

                if (!ignoreExceptions)
                {
                    // Stop execution on first failure
                    throw;
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Executes conditions to determine if seeding should proceed.
    /// </summary>
    /// <param name="conditions">Collection of conditions to evaluate</param>
    /// <param name="context">The seeding context</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of condition evaluation results and overall pass/fail status</returns>
    public async Task<(bool AllPassed, IEnumerable<ConditionResult> Results)> ExecuteConditions(
        IEnumerable<ICondition> conditions,
        SeedingContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ConditionResult>();
        var allPassed = true;

        foreach (var condition in conditions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new ConditionResult
            {
                ConditionType = condition.GetType().FullName ?? condition.GetType().Name
            };

            var startTime = DateTime.UtcNow;

            try
            {
                result.Passed = await condition.ShouldExecute(context, cancellationToken);
                result.Duration = DateTime.UtcNow - startTime;

                if (!result.Passed)
                {
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Duration = DateTime.UtcNow - startTime;
                result.Passed = false; // Failed conditions default to false
                allPassed = false;
            }

            results.Add(result);
        }

        return (allPassed, results);
    }

    /// <summary>
    /// Validates that priorities are configured correctly to avoid execution order issues.
    /// </summary>
    /// <param name="validators">Schema validators to check</param>
    /// <param name="seeders">Data seeders to check</param>
    /// <returns>Validation issues, if any</returns>
    public IEnumerable<string> ValidatePriorities(
        IEnumerable<ISchemaValidator> validators,
        IEnumerable<IDataSeeder> seeders)
    {
        var issues = new List<string>();

        // Check for duplicate priorities within validators
        var validatorPriorities = validators.GroupBy(v => v.Priority)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in validatorPriorities)
        {
            var types = string.Join(", ", group.Select(v => v.GetType().Name));
            issues.Add($"Multiple schema validators have priority {group.Key}: {types}");
        }

        // Check for duplicate priorities within seeders
        var seederPriorities = seeders.GroupBy(s => s.Priority)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in seederPriorities)
        {
            var types = string.Join(", ", group.Select(s => s.GetType().Name));
            issues.Add($"Multiple data seeders have priority {group.Key}: {types}");
        }

        // Check for data seeders with lower priorities than schema validators
        var maxValidatorPriority = validators.Any() ? validators.Max(v => v.Priority) : 0;
        var minSeederPriority = seeders.Any() ? seeders.Min(s => s.Priority) : int.MaxValue;

        if (minSeederPriority <= maxValidatorPriority)
        {
            issues.Add($"Data seeder priority {minSeederPriority} is not higher than max schema validator priority {maxValidatorPriority}. " +
                      "Schema validators should use priorities 1-99, data seeders should use 100+.");
        }

        return issues;
    }

    /// <summary>
    /// Suggests optimal priority values to avoid conflicts.
    /// </summary>
    /// <param name="validators">Existing schema validators</param>
    /// <param name="seeders">Existing data seeders</param>
    /// <returns>Priority suggestions</returns>
    public PrioritySuggestions SuggestPriorities(
        IEnumerable<ISchemaValidator> validators,
        IEnumerable<IDataSeeder> seeders)
    {
        var usedValidatorPriorities = validators.Select(v => v.Priority).ToHashSet();
        var usedSeederPriorities = seeders.Select(s => s.Priority).ToHashSet();

        var suggestions = new PrioritySuggestions();

        // Suggest next available validator priority (1-99, gaps of 10)
        for (int i = 10; i <= 90; i += 10)
        {
            if (!usedValidatorPriorities.Contains(i))
            {
                suggestions.NextSchemaValidatorPriority = i;
                break;
            }
        }

        // Suggest next available seeder priority (100+, gaps of 10)
        for (int i = 100; i <= 1000; i += 10)
        {
            if (!usedSeederPriorities.Contains(i))
            {
                suggestions.NextDataSeederPriority = i;
                break;
            }
        }

        return suggestions;
    }
}

/// <summary>
/// Contains priority suggestions for new components.
/// </summary>
public class PrioritySuggestions
{
    /// <summary>
    /// Suggested priority for the next schema validator.
    /// </summary>
    public int? NextSchemaValidatorPriority { get; set; }

    /// <summary>
    /// Suggested priority for the next data seeder.
    /// </summary>
    public int? NextDataSeederPriority { get; set; }
}

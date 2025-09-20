using System;
using System.Collections.Generic;
using System.Linq;

namespace FluentCMS.DataSeeding.Models;

/// <summary>
/// Represents the result of a seeding operation, including success status,
/// timing information, and details about what was executed.
/// </summary>
public class SeedingResult
{
    /// <summary>
    /// Gets or sets whether the overall seeding operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the total time taken for the seeding operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets when the seeding operation started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the seeding operation completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Gets the collection of schema validation results.
    /// </summary>
    public List<SchemaValidationResult> SchemaResults { get; } = new();

    /// <summary>
    /// Gets the collection of data seeding results.
    /// </summary>
    public List<DataSeedingResult> DataResults { get; } = new();

    /// <summary>
    /// Gets the collection of condition evaluation results.
    /// </summary>
    public List<ConditionResult> ConditionResults { get; } = new();

    /// <summary>
    /// Gets or sets any exception that occurred during seeding.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets the total number of schema validators that were executed.
    /// </summary>
    public int TotalSchemaValidators => SchemaResults.Count;

    /// <summary>
    /// Gets the total number of data seeders that were executed.
    /// </summary>
    public int TotalDataSeeders => DataResults.Count;

    /// <summary>
    /// Gets the number of schema validators that had to create schemas.
    /// </summary>
    public int SchemasCreated => SchemaResults.Count(r => r.SchemaCreated);

    /// <summary>
    /// Gets the number of data seeders that actually seeded data.
    /// </summary>
    public int DataSeeded => DataResults.Count(r => r.DataSeeded);

    /// <summary>
    /// Gets whether all conditions passed evaluation.
    /// </summary>
    public bool AllConditionsPassed => ConditionResults.All(r => r.Passed);
}

/// <summary>
/// Represents the result of a schema validation operation.
/// </summary>
public class SchemaValidationResult
{
    /// <summary>
    /// Gets or sets the name of the schema validator type.
    /// </summary>
    public string ValidatorType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority of the validator.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether the schema was already valid.
    /// </summary>
    public bool SchemaWasValid { get; set; }

    /// <summary>
    /// Gets or sets whether a schema was created during this operation.
    /// </summary>
    public bool SchemaCreated { get; set; }

    /// <summary>
    /// Gets or sets the time taken for this validation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets any exception that occurred during validation.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets whether this validation was successful.
    /// </summary>
    public bool IsSuccess => Exception == null;
}

/// <summary>
/// Represents the result of a data seeding operation.
/// </summary>
public class DataSeedingResult
{
    /// <summary>
    /// Gets or sets the name of the data seeder type.
    /// </summary>
    public string SeederType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority of the seeder.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether data already existed.
    /// </summary>
    public bool DataAlreadyExists { get; set; }

    /// <summary>
    /// Gets or sets whether data was seeded during this operation.
    /// </summary>
    public bool DataSeeded { get; set; }

    /// <summary>
    /// Gets or sets the time taken for this seeding operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets any exception that occurred during seeding.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets whether this seeding operation was successful.
    /// </summary>
    public bool IsSuccess => Exception == null;
}

/// <summary>
/// Represents the result of a condition evaluation.
/// </summary>
public class ConditionResult
{
    /// <summary>
    /// Gets or sets the name of the condition type.
    /// </summary>
    public string ConditionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the condition passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the time taken to evaluate this condition.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets any exception that occurred during evaluation.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets whether this condition evaluation was successful.
    /// </summary>
    public bool IsSuccess => Exception == null;
}

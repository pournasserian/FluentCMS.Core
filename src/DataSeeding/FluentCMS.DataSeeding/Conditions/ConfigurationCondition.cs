using FluentCMS.DataSeeding.Abstractions;
using Microsoft.Extensions.Configuration;

namespace FluentCMS.DataSeeding.Conditions;

/// <summary>
/// Condition that checks configuration settings to determine if seeding should execute.
/// Compares a configuration value against an expected value using case-insensitive comparison.
/// </summary>
public class ConfigurationCondition(IConfiguration configuration, string configKey, string expectedValue) : ICondition
{
    /// <summary>
    /// Gets the descriptive name of this condition for logging purposes
    /// </summary>
    public string Name => $"Configuration: {configKey} = {expectedValue}";

    /// <summary>
    /// Evaluates whether seeding should execute based on configuration value comparison
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if the configuration value matches the expected value (case-insensitive), false otherwise</returns>
    public Task<bool> ShouldExecute(CancellationToken cancellationToken = default)
    {
        try
        {
            // Retrieve the configuration value using the provided key
            var configValue = configuration[configKey];

            // Compare the actual configuration value with the expected value (case-insensitive)
            return Task.FromResult(string.Equals(configValue, expectedValue, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // Return false if any exception occurs during configuration retrieval
            // This ensures seeding is skipped when configuration is unavailable or invalid
            return Task.FromResult(false);
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Abstractions;
using FluentCMS.DataSeeding.Models;

namespace FluentCMS.DataSeeding.Conditions;

/// <summary>
/// A condition that evaluates based on configuration values.
/// Useful for controlling seeding through appsettings.json or environment variables.
/// </summary>
public class ConfigurationCondition : ICondition
{
    private readonly string _configurationKey;
    private readonly Func<string?, bool> _valuePredicate;

    /// <summary>
    /// Initializes a new instance of ConfigurationCondition with a configuration key and predicate.
    /// </summary>
    /// <param name="configurationKey">The configuration key to check</param>
    /// <param name="valuePredicate">Function that takes the configuration value and returns whether seeding should proceed</param>
    public ConfigurationCondition(string configurationKey, Func<string?, bool> valuePredicate)
    {
        _configurationKey = configurationKey ?? throw new ArgumentNullException(nameof(configurationKey));
        _valuePredicate = valuePredicate ?? throw new ArgumentNullException(nameof(valuePredicate));
    }

    /// <summary>
    /// Creates a condition that requires a configuration key to be set to "true" (case-insensitive).
    /// </summary>
    /// <param name="configurationKey">The configuration key to check</param>
    /// <returns>A condition that passes when the configuration value is "true"</returns>
    public static ConfigurationCondition IsTrue(string configurationKey)
    {
        return new ConfigurationCondition(configurationKey, value =>
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a condition that requires a configuration key to be set to "false" (case-insensitive).
    /// </summary>
    /// <param name="configurationKey">The configuration key to check</param>
    /// <returns>A condition that passes when the configuration value is "false"</returns>
    public static ConfigurationCondition IsFalse(string configurationKey)
    {
        return new ConfigurationCondition(configurationKey, value =>
            string.Equals(value, "false", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a condition that requires a configuration key to exist (not null or empty).
    /// </summary>
    /// <param name="configurationKey">The configuration key to check</param>
    /// <returns>A condition that passes when the configuration value exists</returns>
    public static ConfigurationCondition Exists(string configurationKey)
    {
        return new ConfigurationCondition(configurationKey, value =>
            !string.IsNullOrEmpty(value));
    }

    /// <summary>
    /// Creates a condition that requires a configuration key to not exist (null or empty).
    /// </summary>
    /// <param name="configurationKey">The configuration key to check</param>
    /// <returns>A condition that passes when the configuration value does not exist</returns>
    public static ConfigurationCondition NotExists(string configurationKey)
    {
        return new ConfigurationCondition(configurationKey, value =>
            string.IsNullOrEmpty(value));
    }

    /// <summary>
    /// Creates a condition that requires a configuration key to equal a specific value.
    /// </summary>
    /// <param name="configurationKey">The configuration key to check</param>
    /// <param name="expectedValue">The expected value</param>
    /// <param name="ignoreCase">Whether to ignore case when comparing</param>
    /// <returns>A condition that passes when the configuration value equals the expected value</returns>
    public static ConfigurationCondition Equals(string configurationKey, string expectedValue, bool ignoreCase = true)
    {
        return new ConfigurationCondition(configurationKey, value =>
            string.Equals(value, expectedValue, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
    }

    /// <summary>
    /// Creates a condition that requires a configuration key to contain a specific substring.
    /// </summary>
    /// <param name="configurationKey">The configuration key to check</param>
    /// <param name="substring">The substring to search for</param>
    /// <param name="ignoreCase">Whether to ignore case when searching</param>
    /// <returns>A condition that passes when the configuration value contains the substring</returns>
    public static ConfigurationCondition Contains(string configurationKey, string substring, bool ignoreCase = true)
    {
        return new ConfigurationCondition(configurationKey, value =>
            value != null && value.Contains(substring, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
    }

    /// <summary>
    /// Creates a condition that requires a configuration key to match one of several allowed values.
    /// </summary>
    /// <param name="configurationKey">The configuration key to check</param>
    /// <param name="allowedValues">Array of allowed values</param>
    /// <returns>A condition that passes when the configuration value matches one of the allowed values</returns>
    public static ConfigurationCondition OneOf(string configurationKey, params string[] allowedValues)
    {
        if (allowedValues == null || allowedValues.Length == 0)
            throw new ArgumentException("At least one allowed value must be specified", nameof(allowedValues));

        return new ConfigurationCondition(configurationKey, value =>
        {
            if (value == null) return false;
            
            foreach (var allowed in allowedValues)
            {
                if (string.Equals(value, allowed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        });
    }

    /// <summary>
    /// Evaluates whether seeding should proceed based on the configuration value.
    /// </summary>
    /// <param name="context">The seeding context providing access to services</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if seeding should proceed, false otherwise</returns>
    public Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var configurationValue = GetConfigurationValue(context);
            return Task.FromResult(_valuePredicate(configurationValue));
        }
        catch (Exception)
        {
            // If we can't read configuration, default to not executing for safety
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Gets the configuration value using reflection to avoid compile-time dependencies.
    /// </summary>
    private string? GetConfigurationValue(SeedingContext context)
    {
        try
        {
            // Try to get IConfiguration service
            var configuration = GetConfigurationService(context);
            if (configuration != null)
            {
                return GetConfigurationValueFromService(configuration);
            }

            // Fallback to environment variables
            return Environment.GetEnvironmentVariable(_configurationKey);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the IConfiguration service using reflection.
    /// </summary>
    private object? GetConfigurationService(SeedingContext context)
    {
        try
        {
            // Look for Microsoft.Extensions.Configuration.IConfiguration
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var configurationType = assembly.GetType("Microsoft.Extensions.Configuration.IConfiguration");
                if (configurationType != null)
                {
                    var getServiceMethod = context.GetType().GetMethod("GetService");
                    if (getServiceMethod != null)
                    {
                        var genericMethod = getServiceMethod.MakeGenericMethod(configurationType);
                        return genericMethod.Invoke(context, null);
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the configuration value from the IConfiguration service using reflection.
    /// </summary>
    private string? GetConfigurationValueFromService(object configuration)
    {
        try
        {
            // IConfiguration has an indexer property
            var indexerProperty = configuration.GetType().GetProperty("Item", new[] { typeof(string) });
            if (indexerProperty != null)
            {
                return indexerProperty.GetValue(configuration, new object[] { _configurationKey }) as string;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

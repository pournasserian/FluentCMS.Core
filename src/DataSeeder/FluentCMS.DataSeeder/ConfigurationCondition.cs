namespace FluentCMS.DataSeeder;

/// <summary>
/// Condition that checks configuration settings
/// </summary>
public class ConfigurationCondition(IConfiguration configuration, string configKey, string expectedValue) : ISeedingCondition
{
    public string Name => $"Configuration: {configKey} = {expectedValue}";

    public Task<bool> ShouldSeed()
    {
        try
        {
            var configValue = configuration[configKey];
            return Task.FromResult(string.Equals(configValue, expectedValue, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
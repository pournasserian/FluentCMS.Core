namespace FluentCMS.Providers;

/// <summary>
/// Root configuration for all providers
/// </summary>
public class ProvidersConfiguration
{
    /// <summary>
    /// Active provider mappings (provider type -> active instance name)
    /// </summary>
    public Dictionary<string, string> Providers { get; set; } = [];

    /// <summary>
    /// All provider instance configurations
    /// </summary>
    public Dictionary<string, ProviderConfiguration> Instances { get; set; } = [];
}


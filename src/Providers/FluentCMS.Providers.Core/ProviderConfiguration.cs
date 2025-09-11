namespace FluentCMS.Providers;

/// <summary>
/// Configuration for a provider instance
/// </summary>
public class ProviderConfiguration
{
    /// <summary>
    /// The full type name of the provider implementation
    /// </summary>
    public string ImplementationType { get; set; } = string.Empty;

    /// <summary>
    /// Additional configuration properties for the provider
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}


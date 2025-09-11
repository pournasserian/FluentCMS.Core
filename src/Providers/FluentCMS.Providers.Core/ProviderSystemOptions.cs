namespace FluentCMS.Providers;

/// <summary>
/// Options for configuring the provider system
/// </summary>
public class ProviderSystemOptions
{
    /// <summary>
    /// Enable hot reload of provider configurations
    /// </summary>
    public bool EnableHotReload { get; set; } = false;

    /// <summary>
    /// Enable health checks for providers
    /// </summary>
    public bool EnableHealthChecks { get; set; } = false;

    /// <summary>
    /// Throw exception when a provider is not found instead of returning null
    /// </summary>
    public bool ThrowOnMissingProvider { get; set; } = true;
}
namespace FluentCMS.Providers;

/// <summary>
/// Options for configuring the provider system
/// </summary>
public class ProviderSystemOptions
{
    /// <summary>
    /// Throw exception when a provider is not found instead of returning null
    /// </summary>
    public bool ThrowOnMissingProvider { get; set; } = true;
}
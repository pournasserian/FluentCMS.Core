namespace FluentCMS.Providers.Options;

/// <summary>
/// Configuration options for the provider system.
/// </summary>
public class ProviderSystemOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether provider health monitoring is enabled.
    /// </summary>
    public bool EnableHealthMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in seconds between health checks.
    /// </summary>
    public int HealthCheckInterval { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets the directory path where provider assemblies are stored.
    /// </summary>
    public string ProviderDirectory { get; set; } = "Providers";

    /// <summary>
    /// Gets or sets the default provider implementations to activate on startup.
    /// Key is the provider type interface name, value is the provider implementation ID.
    /// </summary>
    public Dictionary<string, string> DefaultProviders { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets a value indicating whether to automatically load provider assemblies
    /// that are detected in the provider directory on startup.
    /// </summary>
    public bool AutoLoadProviders { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of concurrent provider operations.
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 5;

    /// <summary>
    /// Gets or sets the timeout in seconds for provider operations.
    /// </summary>
    public int OperationTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to fallback to the previous active provider
    /// if activation of a new provider fails.
    /// </summary>
    public bool FallbackOnActivationFailure { get; set; } = true;
}

namespace FluentCMS.Providers.Data.Models;

/// <summary>
/// Represents configuration for a provider implementation in the database.
/// </summary>
public class ProviderConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider configuration.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the provider implementation ID.
    /// </summary>
    public string ImplementationId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JSON serialized configuration.
    /// </summary>
    public string ConfigurationJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the last update timestamp of the provider configuration.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the provider implementation.
    /// </summary>
    public virtual ProviderImplementation? Implementation { get; set; }
}

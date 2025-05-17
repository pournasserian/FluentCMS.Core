namespace FluentCMS.Providers.Data.Models;

/// <summary>
/// Represents a specific implementation of a provider interface in the database.
/// </summary>
public class ProviderImplementation
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider implementation.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the provider type ID.
    /// </summary>
    public string ProviderTypeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the provider implementation.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the provider implementation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the version of the provider implementation.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the full .NET type name of the provider implementation.
    /// </summary>
    public string FullTypeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path to the assembly containing the provider implementation.
    /// </summary>
    public string AssemblyPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the provider implementation is installed.
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider implementation is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the health status of the provider implementation.
    /// </summary>
    public ProviderHealthStatus HealthStatus { get; set; } = ProviderHealthStatus.Healthy;

    /// <summary>
    /// Gets or sets the health status message of the provider implementation.
    /// </summary>
    public string? HealthMessage { get; set; }

    /// <summary>
    /// Gets or sets the last health check timestamp of the provider implementation.
    /// </summary>
    public DateTimeOffset? LastHealthCheckAt { get; set; }

    /// <summary>
    /// Gets or sets the installation timestamp of the provider implementation.
    /// </summary>
    public DateTimeOffset? InstalledAt { get; set; }

    /// <summary>
    /// Gets or sets the activation timestamp of the provider implementation.
    /// </summary>
    public DateTimeOffset? ActivatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp of the provider implementation.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the provider type.
    /// </summary>
    public virtual ProviderType? ProviderType { get; set; }
}

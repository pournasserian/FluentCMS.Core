namespace FluentCMS.Providers.Entities;

/// <summary>
/// Represents a specific implementation of a provider interface in the database.
/// </summary>
public class ProviderImplementation : AuditableEntity
{
    /// <summary>
    /// Gets or sets the provider type ID.
    /// </summary>
    public Guid ProviderTypeId { get; set; }

    /// <summary>
    /// Gets or sets the name of the provider implementation.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the description of the provider implementation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the full .NET type name of the provider implementation.
    /// </summary>
    public string FullTypeName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the path to the assembly containing the provider implementation.
    /// </summary>
    public string AssemblyPath { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the provider implementation is installed.
    /// </summary>
    public bool IsInstalled { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the provider implementation is active.
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Gets or sets the installation timestamp of the provider implementation.
    /// </summary>
    public DateTime? InstalledAt { get; set; }

    /// <summary>
    /// Gets or sets the activation timestamp of the provider implementation.
    /// </summary>
    public DateTime? ActivatedAt { get; set; }
}

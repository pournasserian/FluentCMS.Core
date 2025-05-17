namespace FluentCMS.Providers.Data.Models;

/// <summary>
/// Represents a provider interface type in the database.
/// </summary>
public class ProviderType
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider type.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the provider type.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the display name of the provider type.
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the full .NET type name of the provider interface.
    /// </summary>
    public string FullTypeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the assembly name containing the provider interface.
    /// </summary>
    public string AssemblyName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the creation timestamp of the provider type.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp of the provider type.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

namespace FluentCMS.Providers.Entities;

/// <summary>
/// Represents a provider interface type in the database.
/// </summary>
public class ProviderType : AuditableEntity
{
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
}

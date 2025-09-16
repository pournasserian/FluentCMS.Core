namespace FluentCMS.Providers.Repositories.Abstractions;

/// <summary>
/// Represents a provider instance stored in the database.
/// </summary>
public class Provider : AuditableEntity
{
    /// <summary>
    /// Unique name for the provider within its area.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The display name of this provider for administrative purposes.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The functional area this provider belongs to (e.g., "Email", "VirtualFile").
    /// </summary>
    public string Area { get; set; } = string.Empty;

    /// <summary>
    /// Full type name of the provider's module class.
    /// </summary>
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider is currently active.
    /// Only one provider per area can be active at a time.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// JSON string for the provider options.
    /// </summary>
    public string Options { get; set; } = string.Empty;
}

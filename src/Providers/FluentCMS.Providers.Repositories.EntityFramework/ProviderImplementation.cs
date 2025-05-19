namespace FluentCMS.Providers.Repositories.EntityFramework;

/// <summary>
/// Represents a specific implementation of a provider interface in the database.
/// </summary>
public class ProviderImplementation : AuditableEntity
{
    public Guid ProviderTypeId { get; set; }
    public bool IsInstalled { get; set; } = false;
    public bool IsActive { get; set; } = false;
    public DateTime? InstalledAt { get; set; }
    public DateTime? ActivatedAt { get; set; }

    public string Category { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string ImplemetationVersion { get; set; } = default!;
    public bool IsDefault { get; set; } 
    public string TypeName { get; set; } = default!;
    public string AssemblyFile { get; set; } = default!;
    public string AssemblyName { get; set; } = default!;
}

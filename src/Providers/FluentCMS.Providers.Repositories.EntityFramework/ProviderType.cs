namespace FluentCMS.Providers.Repositories.EntityFramework;

/// <summary>
/// Represents a provider interface type in the database.
/// </summary>
public class ProviderType : AuditableEntity
{
    public string TypeName { get; set; } = default!;
    public string AssemblyName { get; set; } = default!;
    public string AssemblyFile { get; set; } = default!;
}

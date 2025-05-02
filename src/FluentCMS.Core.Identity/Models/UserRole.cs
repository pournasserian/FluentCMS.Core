namespace FluentCMS.Core.Identity.Models;

public class UserRole : IdentityUserRole<Guid>, IAuditableEntity
{
    // IAuditableEntity implementations
    public Guid Id { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int Version { get; set; }
}

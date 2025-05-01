namespace FluentCMS.Plugins.Authentication.Models;

public class Role : IdentityRole<Guid>, IAuditableEntity
{
    // IAuditableEntity implementations
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int Version { get; set; }

    // Claims collection
    public ICollection<RoleClaim> Claims { get; set; } = [];

    public Role()
    {
    }

    public Role(string roleName) : this()
    {
        Name = roleName;
    }
}
namespace FluentCMS.Plugins.Authentication.Models;

public class User : IdentityUser<Guid>, IAuditableEntity
{
    // IAuditableEntity implementations
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int Version { get; set; }

    // Additional properties
    public DateTime? LoginAt { get; set; }
    public int LoginCount { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public string? PasswordChangedBy { get; set; }
    public bool Enabled { get; set; } = true;
    public string? AuthenticatorKey { get; set; }

    public User()
    {
    }

    public User(string userName) : base(userName)
    {
    }
}

namespace FluentCMS.Plugins.Authentication.Models;

public class User : User<UserClaim, UserLogin, UserToken>
{
}

public class User<TUserClaim, TUserLogin, TUserToken> : IdentityUser<Guid>, IAuditableEntity
    where TUserClaim : UserClaim, new()
    where TUserLogin : UserLogin, new()
    where TUserToken : UserToken, new()
{
    // IAuditableEntity implementations
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int Version { get; set; }

    // Additional properties
    public DateTime? LastLogin { get; set; }
    public int LoginCount { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public string? PasswordChangedBy { get; set; }
    public bool Enabled { get; set; } = true;
    public string? AuthenticatorKey { get; set; }

    // Claims collection
    public ICollection<TUserClaim> Claims { get; set; } = [];

    // Logins collection
    public ICollection<TUserLogin> Logins { get; set; } = [];

    // Tokens collection
    public ICollection<TUserToken> Tokens { get; set; } = [];

    public User()
    {
    }

    public User(string userName) : base(userName)
    {
    }
}

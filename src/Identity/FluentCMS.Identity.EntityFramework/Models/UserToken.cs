namespace FluentCMS.Identity.Models;

public class UserToken : IdentityUserToken<Guid>, IEntity
{
    public Guid Id { get; set; }
}
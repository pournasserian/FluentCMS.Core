namespace FluentCMS.Plugins.Authentication.Models;

public class UserTwoFactorRecoveryCode
{
    public required string Code { get; set; }
    public bool Redeemed { get; set; }
}

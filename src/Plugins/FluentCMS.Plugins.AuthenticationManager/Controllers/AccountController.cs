namespace FluentCMS.Plugins.AuthenticationManager.Controllers;

public class AccountController(IOptions<JwtOptions> jwtOptions, IOptions<IdentityOptions> identityOptions, UserManager<User> userManager) : BaseController
{

    [HttpGet]
    public JwtOptions GetJwt()
    {
        return jwtOptions.Value;
    }

    [HttpGet]
    public async Task GetAll()
    {
        await userManager.CreateAsync(new User
        {
            Email = "ap@momentaj.com",
            EmailConfirmed = true,
            Enabled = true,
            UserName = "test",
        }, "asdfDFDF134##$");
    }

    [HttpGet]
    public IdentityOptions GetIdentity()
    {
        return identityOptions.Value;
    }
}

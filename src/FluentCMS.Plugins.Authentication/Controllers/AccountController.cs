using FluentCMS.Core.Identity.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FluentCMS.Plugins.Authentication.Controllers;


public class AccountController(IOptions<JwtOptions> jwtOptions, IOptions<IdentityOptions> identityOptions) : BaseController
{

    [HttpGet]
    public JwtOptions GetJwt()
    {
        return jwtOptions.Value;
    }

    [HttpGet]
    public IdentityOptions GetIdentity()
    {
        return identityOptions.Value;
    }
}

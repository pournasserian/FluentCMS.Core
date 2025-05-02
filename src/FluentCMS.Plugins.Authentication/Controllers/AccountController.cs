using FluentCMS.Core.Identity.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FluentCMS.Plugins.Authentication.Controllers;

[ApiController]
[Route("api/{controller}/{action}")]
public class AccountController(IOptions<JwtOptions> jwtOptions, IOptions<IdentityOptions> identityOptions) : ControllerBase
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

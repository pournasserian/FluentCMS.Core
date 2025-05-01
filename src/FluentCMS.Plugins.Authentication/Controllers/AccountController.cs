using FluentCMS.Plugins.Authentication.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FluentCMS.Plugins.Authentication.Controllers;

[ApiController]
[Route("api/{controller}/{action}")]
public class AccountController(ILogger<AccountController> logger, IOptions<JwtOptions> options) : ControllerBase
{

    [HttpGet]
    public JwtOptions Get()
    {
        return options.Value;
    }
}

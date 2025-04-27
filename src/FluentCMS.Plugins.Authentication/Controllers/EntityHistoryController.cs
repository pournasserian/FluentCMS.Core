using FluentCMS.Plugins.Authentication.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentCMS.Plugins.Authentication.Controllers;

[ApiController]
[Route("api/history")]
public class AuthController(ILogger<AuthController> logger, IOptions<JwtOptions> options) : ControllerBase
{

    [HttpGet]
    public JwtOptions Get()
    {
        return options.Value;
    }
}

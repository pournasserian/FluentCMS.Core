using FluentCMS.Providers.Email.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]/[action]")]
public class TestController(IEmailProvider emailProvider) : ControllerBase
{

    [HttpGet]
    public string GetAll()
    {
        return emailProvider.GetType().FullName!;
    }
}

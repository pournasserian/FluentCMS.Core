using FluentCMS.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FluentCMS;

[ApiController]
[Route("api/[controller]/[action]")]
public class TestController
{
    public TestController(IOptionsMonitor<List<ProviderConfig>> options)
    {
        var x = options.CurrentValue;
    }
    [HttpGet]
    public bool GetAll(CancellationToken cancellationToken = default)
    {
        return true;
    }

}
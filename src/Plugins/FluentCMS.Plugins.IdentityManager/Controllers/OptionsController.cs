//using FluentCMS.Options.Services;

//namespace FluentCMS.Plugins.IdentityManager.Controllers;

//[ApiController]
//[Route("api/[controller]/[action]")]
//public class OptionsController(IOptionsService optionsService, IOptionsSnapshot<PasswordOptions> passwordOptions)
//{
//    [HttpGet]
//    public Task<ApiResult<PasswordOptions>> GetPasswordOptions(CancellationToken cancellationToken = default)
//    {
//        return Task.FromResult(new ApiResult<PasswordOptions>(passwordOptions.Value));
//    }

//    [HttpPut]
//    public async Task<ApiResult<PasswordOptions>> UpdatePasswordOptions([FromBody] PasswordOptions options, CancellationToken cancellationToken = default)
//    {
//        // TODO: validation
//        await optionsService.Update("IdentityOptions:Password", options, cancellationToken);
//        return new ApiResult<PasswordOptions>(options);
//    }
//}

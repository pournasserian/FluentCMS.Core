using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Core.Api.Controllers;

[ApiController]
[Produces("application/json")]
//[TypeFilter(typeof(ApiTokenAuthorizeFilter))]
[Route("api/[controller]/[action]")]
public abstract class BaseController : ControllerBase
{
    public static ApiResult<T> Ok<T>(T item)
    {
        return new ApiResult<T>(item);
    }

    public static ApiPagedResult<T> Ok<T>(IEnumerable<T> items, long totalCount, int pageNumber, int pageSize)
    {
        return new ApiPagedResult<T>(items, totalCount, pageNumber, pageSize);
    }
}
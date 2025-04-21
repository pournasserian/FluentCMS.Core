using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FluentCMS.Core.Api.Filters;

public class ApiResultActionFilter(IApplicationExecutionContext execContext) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Execute the action
        var executedContext = await next();

        // Check if the action returns a value
        if (executedContext.Result is ObjectResult result && context.ActionDescriptor.IsApiResultType())
        {
            // Get the value returned by the action
            var value = result.Value;

            if (value == null)
                return;

            var apiResult = (IApiResult)value;
            apiResult.Duration = (DateTime.UtcNow - execContext.StartDate).TotalMilliseconds;
            apiResult.SessionId = execContext.SessionId;
            apiResult.TraceId = execContext.TraceId;
            apiResult.UniqueId = execContext.UniqueId;
            apiResult.Status = 200;
            apiResult.IsSuccess = true;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FluentCMS.Core.Api.Filters;

public class ApiResultExceptionFilter(ApiExecutionContext executionContext) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (!context.ActionDescriptor.IsApiResultType())
            return;

        var apiResult = new ApiResult
        {
            Duration = (DateTime.UtcNow - executionContext.StartDate).TotalMilliseconds,
            SessionId = executionContext.SessionId,
            TraceId = executionContext.TraceId,
            UniqueId = executionContext.UniqueId,
            Status = 500,
            IsSuccess = false,
        };

        var exception = context.Exception;
        apiResult.Errors.Add(new ApiError { Code = "Unknown", Description = exception.Message });

        context.Result = new ObjectResult(apiResult)
        {
            StatusCode = apiResult.Status
        };
    }
}

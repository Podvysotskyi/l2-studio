using L2.Studio.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace L2.Studio.Api.Filters;

public sealed class ValidateDirectoryRequestAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.Values.OfType<DirectoryRequest>().FirstOrDefault() is not { } request)
        {
            return;
        }

        var errors = new Dictionary<string, string[]>();
        if ((request.Query?.Trim().Length ?? 0) > 100) errors["query"] = ["Search terms must contain 100 characters or fewer."];
        if (request.Page < 1) errors["page"] = ["Page must be at least 1."];
        if (request.PageSize is < 1 or > 100) errors["pageSize"] = ["Page size must be between 1 and 100."];
        if (errors.Count > 0) context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
    }
}

using L2.Studio.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace L2.Studio.Api.Filters;

public sealed class ValidateAssetCatalogRequestAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.Values.OfType<AssetCatalogRequest>().FirstOrDefault() is not { } request) return;
        var errors = new Dictionary<string, string[]>();
        if (request.Page < 1) errors["page"] = ["Page must be positive."];
        if (request.PageSize is < 1 or > 500) errors["pageSize"] = ["Page size must be between 1 and 500."];
        if (errors.Count > 0) context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
    }
}

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
        if (request is NpcDirectoryRequest npcRequest)
        {
            ValidateLookupFilter(npcRequest.NpcTypeName, "npcTypeName", "NPC type", errors);
            ValidateLookupFilter(npcRequest.NpcRaceName, "npcRaceName", "NPC race", errors);
            ValidateLookupFilter(npcRequest.NpcSexName, "npcSexName", "NPC sex", errors);
            if (npcRequest.WithoutRace is true && !string.IsNullOrWhiteSpace(npcRequest.NpcRaceName))
                errors["withoutRace"] = ["Choose either a specific NPC race or no race."];
        }
        if (errors.Count > 0) context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
    }

    private static void ValidateLookupFilter(
        string? value,
        string key,
        string label,
        IDictionary<string, string[]> errors)
    {
        if ((value?.Trim().Length ?? 0) > 64)
            errors[key] = [$"{label} filters must contain 64 characters or fewer."];
    }
}

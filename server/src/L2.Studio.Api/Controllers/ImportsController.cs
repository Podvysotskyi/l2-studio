using L2.Studio.Contracts.Requests;
using L2.Studio.Contracts.Responses;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/game-versions/{gameVersion}/imports")]
public sealed class ImportsController(IImportJobRepository repository) : ControllerBase
{
    [HttpPost("content/{target}")]
    public async Task<ActionResult<ImportJobSummary>> QueueContent(
        string gameVersion,
        string target,
        [FromBody] ContentImportRequest? request,
        CancellationToken cancellationToken)
    {
        if (!ContentImportTargetValues.All.Contains(target)) return NotFound();
        if (!ContentImportTargetValues.Supports(gameVersion, target))
            return ValidationError("gameVersion", $"The '{target}' import does not support '{gameVersion}'.");
        var mode = request?.Mode ?? ImportJobValues.AddMissing;
        if (!ImportJobValues.ContentModes.Contains(mode))
            return ValidationError("mode", "Import mode must be add_missing or restore_defaults.");
        var job = await repository.QueueContentAsync(gameVersion, target, mode, cancellationToken);
        return job is null
            ? Conflict(new { message = $"Another {ContentImportTargetValues.Family(target)} content import is active." })
            : Accepted($"/api/game-versions/{gameVersion}/imports/{job.Id}", job);
    }

    [HttpGet]
    public async Task<ActionResult<ImportJobPage>> List(
        string gameVersion,
        [FromQuery] string? category,
        [FromQuery] string? target,
        [FromQuery] string? status,
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (category is not null && category is not (ImportJobValues.Content or ImportJobValues.Asset))
            return ValidationError("category", "Category must be content or asset.");
        if (status is not null && !ImportJobValues.Statuses.Contains(status))
            return ValidationError("status", "Import status is invalid.");
        if (page < 1) return ValidationError("page", "Page must be at least 1.");
        if (pageSize is < 1 or > 100) return ValidationError("pageSize", "Page size must be between 1 and 100.");
        return Ok(await repository.GetPageAsync(
            gameVersion, category, target, status, query, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ImportJobSummary>> Get(
        string gameVersion,
        Guid id,
        CancellationToken cancellationToken)
    {
        var job = await repository.GetAsync(gameVersion, id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    private BadRequestObjectResult ValidationError(string key, string message) =>
        BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [key] = [message] }));
}

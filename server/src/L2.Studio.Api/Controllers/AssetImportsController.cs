using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Exceptions;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/assets/{kind}/imports")]
public sealed class AssetImportsController(IAssetImportRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AssetImportJobSummary>> Queue(string kind, [FromQuery] AssetImportRequest request, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        var levelName = string.IsNullOrWhiteSpace(request.LevelName) ? null : request.LevelName.Trim();
        if (levelName is not null && kind != AssetImportJobValues.LevelPreviews)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["levelName"] = ["A level target is supported only for level-preview imports."] }));
        if (levelName is { Length: > 128 } || levelName?.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-') == true)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["levelName"] = ["Level name must contain only ASCII letters, digits, underscores, or hyphens."] }));
        try
        {
            var job = await repository.QueueAsync(kind, levelName, token);
            return job is null
                ? Conflict(new { message = $"An import for '{kind}' is already queued or running." })
                : Accepted($"/api/assets/{kind}/imports/{job.Id}", job);
        }
        catch (AssetImportTargetNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetImportJobSummary>>> List(string kind, [FromQuery] int limit = 20, CancellationToken token = default)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        if (limit is < 1 or > 100) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["limit"] = ["Limit must be between 1 and 100."] }));
        return Ok(await repository.GetRecentAsync(kind, limit, token));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetImportJobSummary>> Get(string kind, Guid id, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        var job = await repository.GetAsync(id, kind, token);
        return job is null ? NotFound() : Ok(job);
    }
}

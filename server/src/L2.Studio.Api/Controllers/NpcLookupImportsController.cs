using L2.Studio.Contracts;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/game-versions/{gameVersion}/content/{kind}/imports")]
public sealed class NpcLookupImportsController(INpcLookupImportRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<NpcLookupImportRunSummary>> Queue(
        string gameVersion,
        string kind,
        CancellationToken token)
    {
        if (!Supported(gameVersion, kind)) return NotFound();
        var run = await repository.QueueAsync(gameVersion, kind, token);
        return run is null
            ? Conflict(new { message = $"An import for '{kind}' is already active for '{gameVersion}'." })
            : Accepted($"/api/game-versions/{gameVersion}/content/{kind}/imports/{run.Id}", run);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NpcLookupImportRunSummary>>> List(
        string gameVersion,
        string kind,
        [FromQuery] int limit = 20,
        CancellationToken token = default)
    {
        if (!Supported(gameVersion, kind)) return NotFound();
        if (limit is < 1 or > 100)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["limit"] = ["Limit must be between 1 and 100."]
            }));
        }

        return Ok(await repository.GetRecentAsync(gameVersion, kind, limit, token));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NpcLookupImportRunSummary>> Get(
        string gameVersion,
        string kind,
        Guid id,
        CancellationToken token)
    {
        if (!Supported(gameVersion, kind)) return NotFound();
        var run = await repository.GetAsync(gameVersion, kind, id, token);
        return run is null ? NotFound() : Ok(run);
    }

    private static bool Supported(string gameVersion, string kind) =>
        NpcLookupImportJobValues.SupportedGameVersions.Contains(gameVersion) &&
        NpcLookupImportJobValues.SupportedKinds.Contains(kind);
}

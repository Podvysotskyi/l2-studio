using L2.Studio.Contracts.Requests;
using L2.Studio.Contracts.Responses;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/game-versions/{gameVersion}/content/items/imports")]
public sealed class ItemImportsController(IItemImportRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ItemImportRunSummary>> Queue(string gameVersion, [FromBody] ItemImportRequest? request, CancellationToken token)
    {
        if (gameVersion != "c1") return NotFound();
        var mode = request?.Mode ?? ItemImportJobValues.AddMissing;
        if (!ItemImportJobValues.SupportedModes.Contains(mode)) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["mode"] = ["Import mode is invalid."] }));
        var run = await repository.QueueAsync(gameVersion, mode, token);
        return run is null ? Conflict(new { message = "An item import is already active for C1." }) : Accepted($"/api/game-versions/{gameVersion}/content/items/imports/{run.Id}", run);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemImportRunSummary>>> List(string gameVersion, [FromQuery] int limit = 20, CancellationToken token = default)
    {
        if (gameVersion != "c1") return NotFound();
        if (limit is < 1 or > 100) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["limit"] = ["Limit must be between 1 and 100."] }));
        return Ok(await repository.GetRecentAsync(gameVersion, limit, token));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemImportRunSummary>> Get(string gameVersion, Guid id, CancellationToken token)
    {
        if (gameVersion != "c1") return NotFound();
        var run = await repository.GetAsync(gameVersion, id, token);
        return run is null ? NotFound() : Ok(run);
    }
}

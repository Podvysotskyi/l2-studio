using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/game-versions/{gameVersion}/asset-releases")]
public sealed class AssetReleasesController(IAssetReleaseRepository releases) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AssetReleasePage>> List(
        string gameVersion, [FromQuery] string? status, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken token = default)
    {
        if (status is not null && status is not "draft" and not "published" and not "active" and not "retired")
            return Validation("status", "Release status is invalid.");
        if (page < 1) return Validation("page", "Page must be at least 1.");
        if (pageSize is < 1 or > 100) return Validation("pageSize", "Page size must be between 1 and 100.");
        return Ok(await releases.ListAsync(gameVersion, status, page, pageSize, token));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetReleaseDetail>> Get(string gameVersion, Guid id, CancellationToken token) =>
        await releases.GetAsync(gameVersion, id, token) is { } release ? Ok(release) : NotFound();

    [HttpPost]
    public async Task<ActionResult<AssetReleaseDetail>> Create(
        string gameVersion, CreateAssetReleaseRequest request, CancellationToken token)
    {
        try
        {
            var release = await releases.CreateAsync(gameVersion, request, token);
            return CreatedAtAction(nameof(Get), new { gameVersion, id = release.Release.Id }, release);
        }
        catch (ArgumentException exception) { return Validation("request", exception.Message); }
    }

    [HttpPost("{id:guid}/clone")]
    public async Task<ActionResult<AssetReleaseDetail>> Clone(
        string gameVersion, Guid id, CreateAssetReleaseRequest request, CancellationToken token) =>
        await Execute(() => releases.CloneAsync(gameVersion, id, request, token));

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<AssetReleaseDetail>> Update(
        string gameVersion, Guid id, UpdateAssetReleaseRequest request, CancellationToken token) =>
        await Execute(() => releases.UpdateAsync(gameVersion, id, request, token));

    [HttpPost("{id:guid}/refresh")]
    public async Task<ActionResult<AssetReleaseDetail>> Refresh(string gameVersion, Guid id, CancellationToken token) =>
        await Execute(() => releases.RefreshAsync(gameVersion, id, token));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(string gameVersion, Guid id, CancellationToken token)
    {
        try { return await releases.DeleteDraftAsync(gameVersion, id, token) ? NoContent() : NotFound(); }
        catch (InvalidOperationException exception) { return Conflict(new ProblemDetails { Detail = exception.Message }); }
    }

    [HttpPost("{id:guid}/validate")]
    public async Task<ActionResult<AssetReleaseDetail>> Validate(string gameVersion, Guid id, CancellationToken token) =>
        await Execute(() => releases.QueueValidationAsync(gameVersion, id, token), accepted: true);

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<AssetReleaseDetail>> Publish(string gameVersion, Guid id, CancellationToken token) =>
        await Execute(() => releases.PublishAsync(gameVersion, id, token));

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<AssetReleaseDetail>> Activate(string gameVersion, Guid id, CancellationToken token) =>
        await Execute(() => releases.QueueActivationAsync(gameVersion, id, token), accepted: true);

    [HttpPost("{id:guid}/retire")]
    public async Task<ActionResult<AssetReleaseDetail>> Retire(string gameVersion, Guid id, CancellationToken token) =>
        await Execute(() => releases.RetireAsync(gameVersion, id, token));

    [HttpGet("{id:guid}/resources")]
    public async Task<ActionResult<AssetReleaseResourcePage>> Resources(
        string gameVersion, Guid id, [FromQuery] string type, [FromQuery] string query = "",
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken token = default)
    {
        if (type is not "scene" and not "audio" and not "image") return Validation("type", "Resource type is invalid.");
        if (page < 1 || pageSize is < 1 or > 100) return Validation("page", "Pagination is invalid.");
        var result = await releases.SearchResourcesAsync(gameVersion, id, type, query, page, pageSize, token);
        return result is null ? NotFound() : Ok(result);
    }

    private async Task<ActionResult<AssetReleaseDetail>> Execute(
        Func<Task<AssetReleaseDetail?>> action, bool accepted = false)
    {
        try
        {
            var result = await action();
            if (result is null) return NotFound();
            return accepted ? Accepted(result) : Ok(result);
        }
        catch (ArgumentException exception) { return Validation("request", exception.Message); }
        catch (InvalidOperationException exception) { return Conflict(new ProblemDetails { Detail = exception.Message }); }
    }

    private BadRequestObjectResult Validation(string key, string message) =>
        BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [key] = [message] }));
}

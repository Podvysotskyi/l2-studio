using System.Text.Json;
using L2.Studio.Api.Filters;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/game-versions/{gameVersion}/assets")]
public sealed class AssetCatalogsController(IAssetCatalogRepository repository) : ControllerBase
{
    [HttpGet("catalogs")]
    public Task<IReadOnlyList<AssetCatalogSummary>> GetSummaries(string gameVersion, CancellationToken token) => repository.GetSummariesAsync(gameVersion, token);

    [HttpGet("artifacts")]
    public async Task<ActionResult<AssetArtifactPage>> GetArtifacts(
        string gameVersion,
        [FromQuery] string? kind,
        [FromQuery] string? sourceKey,
        [FromQuery] bool? current,
        [FromQuery] string? integrityStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken token = default)
    {
        if (kind is not null && !AssetImportJobValues.SupportedKinds.Contains(kind))
            return ValidationError("kind", "Asset kind is invalid.");
        if (integrityStatus is not null && integrityStatus is not "healthy" and not "missing" and not "corrupt")
            return ValidationError("integrityStatus", "Integrity status is invalid.");
        if (page < 1) return ValidationError("page", "Page must be at least 1.");
        if (pageSize is < 1 or > 100) return ValidationError("pageSize", "Page size must be between 1 and 100.");
        return Ok(await repository.GetArtifactsAsync(
            gameVersion, kind, sourceKey, current, integrityStatus, page, pageSize, token));
    }

    [HttpGet("artifacts/{id:guid}")]
    public async Task<ActionResult<AssetArtifactDetail>> GetArtifact(
        string gameVersion, Guid id, CancellationToken token)
    {
        var artifact = await repository.GetArtifactAsync(gameVersion, id, token);
        return artifact is null ? NotFound() : Ok(artifact);
    }

    [HttpPost("artifacts/{id:guid}/verify")]
    public async Task<ActionResult<AssetArtifactDetail>> VerifyArtifact(
        string gameVersion, Guid id, CancellationToken token)
    {
        var artifact = await repository.VerifyArtifactAsync(gameVersion, id, token);
        return artifact is null ? NotFound() : Ok(artifact);
    }

    [HttpGet("{kind}/catalog"), ValidateAssetCatalogRequest]
    public async Task<ActionResult<AssetCatalogPage>> Search(string gameVersion, string kind, [FromQuery] AssetCatalogRequest request, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        var result = await repository.SearchAsync(gameVersion, kind, request.Query, request.PackageName, request.OriginalFolder, request.Page, request.PageSize, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{kind}/catalog/{name}")]
    public async Task<ActionResult<JsonElement>> Get(string gameVersion, string kind, string name, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        var result = await repository.GetAsync(gameVersion, kind, name, token);
        return result is null ? NotFound() : Ok(result.Value);
    }

    private BadRequestObjectResult ValidationError(string key, string message) =>
        BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [key] = [message] }));
}

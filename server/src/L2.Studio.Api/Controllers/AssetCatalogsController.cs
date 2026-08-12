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
}

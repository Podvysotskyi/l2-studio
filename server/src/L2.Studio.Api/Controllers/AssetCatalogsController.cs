using System.Text.Json;
using L2.Studio.Api.Filters;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetCatalogsController(IAssetCatalogRepository repository) : ControllerBase
{
    [HttpGet("catalogs")]
    public Task<IReadOnlyList<AssetCatalogSummary>> GetSummaries(CancellationToken token) => repository.GetSummariesAsync(token);

    [HttpGet("{kind}/catalog"), ValidateAssetCatalogRequest]
    public async Task<ActionResult<AssetCatalogPage>> Search(string kind, [FromQuery] AssetCatalogRequest request, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        var result = await repository.SearchAsync(kind, request.Query, request.PackageName, request.Page, request.PageSize, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{kind}/catalog/{name}")]
    public async Task<ActionResult<JsonElement>> Get(string kind, string name, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        var result = await repository.GetAsync(kind, name, token);
        return result is null ? NotFound() : Ok(result.Value);
    }
}

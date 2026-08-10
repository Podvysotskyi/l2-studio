using L2.Studio.Content;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Assets;

public static class AssetCatalogEndpoints
{
    private static readonly HashSet<string> SupportedKinds =
    [
        AssetImportJobValues.SystemTextures, AssetImportJobValues.Textures, AssetImportJobValues.Music,
        AssetImportJobValues.Sounds, AssetImportJobValues.StaticMeshes, AssetImportJobValues.Levels,
        AssetImportJobValues.LevelPreviews, AssetImportJobValues.Scenes
    ];

    public static IEndpointRouteBuilder MapAssetCatalogs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/assets/catalogs", (AssetCatalogRepository repository, CancellationToken token) => repository.GetSummariesAsync(token));
        endpoints.MapGet("/api/assets/{kind}/catalog", SearchAsync);
        endpoints.MapGet("/api/assets/{kind}/catalog/{name}", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> SearchAsync(string kind, AssetCatalogRepository repository,
        [FromQuery] string query = "", [FromQuery] string? packageName = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (!SupportedKinds.Contains(kind)) return Results.NotFound();
        if (page < 1 || pageSize is < 1 or > 500)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["page"] = ["Page must be positive and pageSize must be between 1 and 500."] });
        var result = await repository.SearchAsync(kind, query, packageName, page, pageSize, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetAsync(string kind, string name, AssetCatalogRepository repository, CancellationToken cancellationToken)
    {
        if (!SupportedKinds.Contains(kind)) return Results.NotFound();
        var result = await repository.GetAsync(kind, name, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result.Value);
    }
}

using L2.Studio.Content;
using L2.Studio.Content.Entities;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Assets;

public static class AssetImportEndpoints
{
    private static readonly HashSet<string> SupportedKinds =
    [
        AssetImportJobValues.SystemTextures,
        AssetImportJobValues.Textures,
        AssetImportJobValues.Music,
        AssetImportJobValues.Sounds,
        AssetImportJobValues.StaticMeshes,
        AssetImportJobValues.Levels,
        AssetImportJobValues.LevelPreviews,
        AssetImportJobValues.Scenes
    ];

    public static IEndpointRouteBuilder MapAssetImports(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/assets/{kind}/imports", QueueAsync);
        endpoints.MapGet("/api/assets/{kind}/imports", ListAsync);
        endpoints.MapGet("/api/assets/{kind}/imports/{id:guid}", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> QueueAsync(
        string kind,
        AssetImportRepository repository,
        CancellationToken cancellationToken,
        [FromQuery] string? levelName = null)
    {
        if (!SupportedKinds.Contains(kind))
        {
            return Results.NotFound();
        }

        levelName = string.IsNullOrWhiteSpace(levelName) ? null : levelName.Trim();
        if (levelName is not null && kind != AssetImportJobValues.LevelPreviews)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["levelName"] = ["A level target is supported only for level-preview imports."]
            });
        }
        if (levelName is { Length: > 128 } ||
            levelName?.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-') == true)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["levelName"] = ["Level name must contain only ASCII letters, digits, underscores, or hyphens."]
            });
        }

        AssetImportJob? job;
        try
        {
            job = await repository.QueueAsync(kind, levelName, cancellationToken);
        }
        catch (AssetImportTargetNotFoundException exception)
        {
            return Results.NotFound(new { message = exception.Message });
        }
        return job is null
            ? Results.Conflict(new { message = $"An import for '{kind}' is already queued or running." })
            : Results.Accepted($"/api/assets/{kind}/imports/{job.Id}", AssetImportRepository.ToSummary(job));
    }

    private static async Task<IResult> ListAsync(
        string kind,
        AssetImportRepository repository,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!SupportedKinds.Contains(kind))
        {
            return Results.NotFound();
        }

        if (limit is < 1 or > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["limit"] = ["Limit must be between 1 and 100."]
            });
        }

        return Results.Ok(await repository.GetRecentAsync(kind, limit, cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        string kind,
        Guid id,
        AssetImportRepository repository,
        CancellationToken cancellationToken)
    {
        if (!SupportedKinds.Contains(kind))
        {
            return Results.NotFound();
        }

        var job = await repository.GetAsync(id, kind, cancellationToken);
        return job is null ? Results.NotFound() : Results.Ok(job);
    }
}

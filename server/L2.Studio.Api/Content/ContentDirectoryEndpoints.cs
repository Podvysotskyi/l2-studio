using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Content;

public static class ContentDirectoryEndpoints
{
    public static IEndpointRouteBuilder MapContentDirectory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/content/npcs", SearchNpcsAsync);
        endpoints.MapGet("/api/content/npc-types", GetNpcTypesAsync);
        endpoints.MapGet("/api/content/npc-races", GetNpcRacesAsync);
        endpoints.MapGet("/api/content/npc-sexes", GetNpcSexesAsync);
        endpoints.MapGet("/api/content/player-classes", GetPlayerClassesAsync);
        endpoints.MapGet("/api/content/player-races", GetPlayerRacesAsync);
        endpoints.MapGet("/api/content/player-sexes", GetPlayerSexesAsync);
        endpoints.MapGet("/api/content/skills", SearchSkillsAsync);
        endpoints.MapGet("/api/content/skill-operate-types", GetSkillOperateTypesAsync);
        endpoints.MapGet("/api/content/skill-target-types", GetSkillTargetTypesAsync);
        return endpoints;
    }

    private static async Task<IResult> SearchNpcsAsync(
        ContentDirectoryRepository repository,
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        query = query?.Trim() ?? string.Empty;
        var errors = ValidateDirectoryRequest(query, page, pageSize);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        return Results.Ok(await repository.SearchNpcsAsync(query, page, pageSize, cancellationToken));
    }

    private static async Task<IResult> SearchSkillsAsync(
        ContentDirectoryRepository repository,
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        query = query?.Trim() ?? string.Empty;
        var errors = ValidateDirectoryRequest(query, page, pageSize);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        return Results.Ok(await repository.SearchSkillsAsync(query, page, pageSize, cancellationToken));
    }

    private static Dictionary<string, string[]> ValidateDirectoryRequest(string query, int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (query.Length > 100)
        {
            errors["query"] = ["Search terms must contain 100 characters or fewer."];
        }

        if (page < 1)
        {
            errors["page"] = ["Page must be at least 1."];
        }

        if (pageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        }

        return errors;
    }

    private static async Task<IResult> GetNpcTypesAsync(
        ContentDirectoryRepository repository,
        CancellationToken cancellationToken) =>
        Results.Ok(await repository.GetNpcTypesAsync(cancellationToken));

    private static async Task<IResult> GetNpcRacesAsync(
        ContentDirectoryRepository repository,
        CancellationToken cancellationToken) =>
        Results.Ok(await repository.GetNpcRacesAsync(cancellationToken));

    private static async Task<IResult> GetNpcSexesAsync(
        ContentDirectoryRepository repository,
        CancellationToken cancellationToken) =>
        Results.Ok(await repository.GetNpcSexesAsync(cancellationToken));

    private static async Task<IResult> GetPlayerClassesAsync(
        ContentDirectoryRepository repository,
        CancellationToken cancellationToken) =>
        Results.Ok(await repository.GetPlayerClassesAsync(cancellationToken));

    private static async Task<IResult> GetPlayerRacesAsync(
        ContentDirectoryRepository repository,
        CancellationToken cancellationToken) =>
        Results.Ok(await repository.GetPlayerRacesAsync(cancellationToken));

    private static async Task<IResult> GetPlayerSexesAsync(
        ContentDirectoryRepository repository,
        CancellationToken cancellationToken) =>
        Results.Ok(await repository.GetPlayerSexesAsync(cancellationToken));

    private static async Task<IResult> GetSkillOperateTypesAsync(
        ContentDirectoryRepository repository,
        CancellationToken cancellationToken) =>
        Results.Ok(await repository.GetSkillOperateTypesAsync(cancellationToken));

    private static async Task<IResult> GetSkillTargetTypesAsync(
        ContentDirectoryRepository repository,
        CancellationToken cancellationToken) =>
        Results.Ok(await repository.GetSkillTargetTypesAsync(cancellationToken));
}

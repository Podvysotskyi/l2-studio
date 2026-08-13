using L2.Studio.Api.Filters;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/game-versions/{gameVersion}/content")]
public sealed class ContentDirectoryController(IContentDirectoryRepository repository) : ControllerBase
{
    [HttpGet("npcs"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.NpcDirectoryPage> SearchNpcs(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchNpcsAsync(gameVersion, request.Query?.Trim() ?? string.Empty, request.Page, request.PageSize, token);

    [HttpGet("skills"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.SkillDirectoryPage> SearchSkills(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchSkillsAsync(gameVersion, request.Query?.Trim() ?? string.Empty, request.Page, request.PageSize, token);

    [HttpGet("npc-types")]
    public Task<IReadOnlyList<L2.Studio.Contracts.NpcLookupSummary>> GetNpcTypes(string gameVersion, CancellationToken token) => repository.GetNpcTypesAsync(gameVersion, token);
    [HttpPatch("npc-types/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.NpcLookupSummary>> UpdateNpcType(
        string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) =>
        UpdateNpcLookup(gameVersion, "npc-types", name, request, token);
    [HttpGet("npc-races")]
    public Task<IReadOnlyList<L2.Studio.Contracts.NpcLookupSummary>> GetNpcRaces(string gameVersion, CancellationToken token) => repository.GetNpcRacesAsync(gameVersion, token);
    [HttpPatch("npc-races/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.NpcLookupSummary>> UpdateNpcRace(
        string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) =>
        UpdateNpcLookup(gameVersion, "npc-races", name, request, token);
    [HttpGet("npc-sexes")]
    public Task<IReadOnlyList<L2.Studio.Contracts.NpcLookupSummary>> GetNpcSexes(string gameVersion, CancellationToken token) => repository.GetNpcSexesAsync(gameVersion, token);
    [HttpPatch("npc-sexes/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.NpcLookupSummary>> UpdateNpcSex(
        string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) =>
        UpdateNpcLookup(gameVersion, "npc-sexes", name, request, token);
    [HttpGet("player-classes")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerClassSummary>> GetPlayerClasses(string gameVersion, CancellationToken token) => repository.GetPlayerClassesAsync(gameVersion, token);
    [HttpGet("player-races")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerLookupSummary>> GetPlayerRaces(string gameVersion, CancellationToken token) => repository.GetPlayerRacesAsync(gameVersion, token);
    [HttpGet("player-sexes")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerLookupSummary>> GetPlayerSexes(string gameVersion, CancellationToken token) => repository.GetPlayerSexesAsync(gameVersion, token);
    [HttpGet("skill-operate-types")]
    public Task<IReadOnlyList<L2.Studio.Contracts.SkillLookupSummary>> GetSkillOperateTypes(string gameVersion, CancellationToken token) => repository.GetSkillOperateTypesAsync(gameVersion, token);
    [HttpGet("skill-target-types")]
    public Task<IReadOnlyList<L2.Studio.Contracts.SkillLookupSummary>> GetSkillTargetTypes(string gameVersion, CancellationToken token) => repository.GetSkillTargetTypesAsync(gameVersion, token);

    private async Task<ActionResult<L2.Studio.Contracts.NpcLookupSummary>> UpdateNpcLookup(
        string gameVersion,
        string kind,
        string name,
        UpdateNpcLookupRequest request,
        CancellationToken token)
    {
        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrEmpty(displayName) || displayName.Length > 64)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["displayName"] = ["Display name must contain between 1 and 64 characters."]
            }));
        }

        var result = await repository.UpdateNpcLookupDisplayNameAsync(gameVersion, kind, name, displayName, token);
        return result is null ? NotFound() : Ok(result);
    }
}

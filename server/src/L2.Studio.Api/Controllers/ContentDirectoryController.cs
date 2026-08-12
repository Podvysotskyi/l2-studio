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
    [HttpGet("npc-races")]
    public Task<IReadOnlyList<L2.Studio.Contracts.NpcLookupSummary>> GetNpcRaces(string gameVersion, CancellationToken token) => repository.GetNpcRacesAsync(gameVersion, token);
    [HttpGet("npc-sexes")]
    public Task<IReadOnlyList<L2.Studio.Contracts.NpcLookupSummary>> GetNpcSexes(string gameVersion, CancellationToken token) => repository.GetNpcSexesAsync(gameVersion, token);
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
}

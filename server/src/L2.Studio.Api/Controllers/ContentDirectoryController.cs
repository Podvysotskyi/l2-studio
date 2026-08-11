using L2.Studio.Api.Filters;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/content")]
public sealed class ContentDirectoryController(IContentDirectoryRepository repository) : ControllerBase
{
    [HttpGet("npcs"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.NpcDirectoryPage> SearchNpcs([FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchNpcsAsync(request.Query?.Trim() ?? string.Empty, request.Page, request.PageSize, token);

    [HttpGet("skills"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.SkillDirectoryPage> SearchSkills([FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchSkillsAsync(request.Query?.Trim() ?? string.Empty, request.Page, request.PageSize, token);

    [HttpGet("npc-types")]
    public Task<IReadOnlyList<L2.Studio.Contracts.NpcLookupSummary>> GetNpcTypes(CancellationToken token) => repository.GetNpcTypesAsync(token);
    [HttpGet("npc-races")]
    public Task<IReadOnlyList<L2.Studio.Contracts.NpcLookupSummary>> GetNpcRaces(CancellationToken token) => repository.GetNpcRacesAsync(token);
    [HttpGet("npc-sexes")]
    public Task<IReadOnlyList<L2.Studio.Contracts.NpcLookupSummary>> GetNpcSexes(CancellationToken token) => repository.GetNpcSexesAsync(token);
    [HttpGet("player-classes")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerClassSummary>> GetPlayerClasses(CancellationToken token) => repository.GetPlayerClassesAsync(token);
    [HttpGet("player-races")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerLookupSummary>> GetPlayerRaces(CancellationToken token) => repository.GetPlayerRacesAsync(token);
    [HttpGet("player-sexes")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerLookupSummary>> GetPlayerSexes(CancellationToken token) => repository.GetPlayerSexesAsync(token);
    [HttpGet("skill-operate-types")]
    public Task<IReadOnlyList<L2.Studio.Contracts.SkillLookupSummary>> GetSkillOperateTypes(CancellationToken token) => repository.GetSkillOperateTypesAsync(token);
    [HttpGet("skill-target-types")]
    public Task<IReadOnlyList<L2.Studio.Contracts.SkillLookupSummary>> GetSkillTargetTypes(CancellationToken token) => repository.GetSkillTargetTypesAsync(token);
}

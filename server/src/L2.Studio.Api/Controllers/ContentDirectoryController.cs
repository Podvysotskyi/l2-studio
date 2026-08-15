using L2.Studio.Api.Filters;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/game-versions/{gameVersion}/content")]
public sealed class ContentDirectoryController(IContentDirectoryRepository repository) : ControllerBase
{
    [HttpGet("items"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.ItemDirectoryPage> SearchItems(string gameVersion, [FromQuery] ItemDirectoryRequest request, CancellationToken token) =>
        repository.SearchItemsAsync(gameVersion, request with
        {
            Query = request.Query?.Trim(), ItemTypeName = TrimOptional(request.ItemTypeName),
            ItemActionName = TrimOptional(request.ItemActionName), ItemBodyPartName = TrimOptional(request.ItemBodyPartName),
            ItemMaterialName = TrimOptional(request.ItemMaterialName), ItemCrystalTypeName = TrimOptional(request.ItemCrystalTypeName)
        }, token);

    [HttpGet("items/{id:int}")]
    public async Task<ActionResult<L2.Studio.Contracts.ItemSummary>> GetItem(string gameVersion, int id, CancellationToken token)
    {
        var item = await repository.GetItemAsync(gameVersion, id, token);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPatch("items/{id:int}")]
    public async Task<ActionResult<L2.Studio.Contracts.ItemSummary>> UpdateItem(string gameVersion, int id, UpdateItemRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100 ||
            string.IsNullOrWhiteSpace(request.ItemTypeName) || request.ItemTypeName.Trim().Length > 64)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["definition"] = ["Item name and type are required and must not exceed their maximum length."]
            }));
        try
        {
            var item = await repository.UpdateItemAsync(gameVersion, id, request with
            {
                Name = request.Name.Trim(), ItemTypeName = request.ItemTypeName.Trim()
            }, token);
            return item is null ? NotFound() : Ok(item);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["definition"] = [exception.Message] }));
        }
    }

    [HttpGet("item-types")]
    public Task<IReadOnlyList<L2.Studio.Contracts.ItemLookupSummary>> GetItemTypes(string gameVersion, CancellationToken token) => repository.GetItemLookupsAsync(gameVersion, "item-types", token);
    [HttpPatch("item-types/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemType(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-types", name, request, token);
    [HttpGet("item-actions")]
    public Task<IReadOnlyList<L2.Studio.Contracts.ItemLookupSummary>> GetItemActions(string gameVersion, CancellationToken token) => repository.GetItemLookupsAsync(gameVersion, "item-actions", token);
    [HttpPatch("item-actions/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemAction(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-actions", name, request, token);
    [HttpGet("item-body-parts")]
    public Task<IReadOnlyList<L2.Studio.Contracts.ItemLookupSummary>> GetItemBodyParts(string gameVersion, CancellationToken token) => repository.GetItemLookupsAsync(gameVersion, "item-body-parts", token);
    [HttpPatch("item-body-parts/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemBodyPart(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-body-parts", name, request, token);
    [HttpGet("item-materials")]
    public Task<IReadOnlyList<L2.Studio.Contracts.ItemLookupSummary>> GetItemMaterials(string gameVersion, CancellationToken token) => repository.GetItemLookupsAsync(gameVersion, "item-materials", token);
    [HttpPatch("item-materials/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemMaterial(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-materials", name, request, token);
    [HttpGet("item-crystal-types")]
    public Task<IReadOnlyList<L2.Studio.Contracts.ItemLookupSummary>> GetItemCrystalTypes(string gameVersion, CancellationToken token) => repository.GetItemLookupsAsync(gameVersion, "item-crystal-types", token);
    [HttpPatch("item-crystal-types/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemCrystalType(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-crystal-types", name, request, token);
    [HttpGet("npcs"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.NpcDirectoryPage> SearchNpcs(string gameVersion, [FromQuery] NpcDirectoryRequest request, CancellationToken token) =>
        repository.SearchNpcsAsync(gameVersion, request with
        {
            Query = request.Query?.Trim(),
            NpcTypeName = TrimOptional(request.NpcTypeName),
            NpcRaceName = TrimOptional(request.NpcRaceName),
            NpcSexName = TrimOptional(request.NpcSexName)
        }, token);

    [HttpGet("npcs/{id:int}")]
    public async Task<ActionResult<L2.Studio.Contracts.NpcSummary>> GetNpc(
        string gameVersion,
        int id,
        CancellationToken token)
    {
        var npc = await repository.GetNpcAsync(gameVersion, id, token);
        return npc is null ? NotFound() : Ok(npc);
    }

    [HttpPatch("npcs/{id:int}")]
    public async Task<ActionResult<L2.Studio.Contracts.NpcSummary>> UpdateNpc(
        string gameVersion,
        int id,
        UpdateNpcRequest request,
        CancellationToken token)
    {
        var name = request.Name?.Trim();
        var npcTypeName = request.NpcTypeName?.Trim();
        var npcRaceName = string.IsNullOrWhiteSpace(request.NpcRaceName) ? null : request.NpcRaceName.Trim();
        var npcSexName = request.NpcSexName?.Trim();
        var errors = ValidateNpc(name, request.Level, npcTypeName, npcRaceName, npcSexName);
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(errors));

        try
        {
            var npc = await repository.UpdateNpcAsync(
                gameVersion, id, name!, request.Level, npcTypeName!, npcRaceName, npcSexName!, token);
            return npc is null ? NotFound() : Ok(npc);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["definition"] = [exception.Message]
            }));
        }
    }

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
    [HttpGet("player-faces")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerAppearanceSummary>> GetPlayerFaces(string gameVersion, CancellationToken token) => repository.GetPlayerFacesAsync(gameVersion, token);
    [HttpGet("player-hair-styles")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerAppearanceSummary>> GetPlayerHairStyles(string gameVersion, CancellationToken token) => repository.GetPlayerHairStylesAsync(gameVersion, token);
    [HttpGet("player-hair-colors")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerAppearanceSummary>> GetPlayerHairColors(string gameVersion, CancellationToken token) => repository.GetPlayerHairColorsAsync(gameVersion, token);
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

    private async Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemLookup(
        string gameVersion, string kind, string name, UpdateNpcLookupRequest request, CancellationToken token)
    {
        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrEmpty(displayName) || displayName.Length > 64)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["displayName"] = ["Display name must contain between 1 and 64 characters."] }));
        var result = await repository.UpdateItemLookupDisplayNameAsync(gameVersion, kind, name, displayName, token);
        return result is null ? NotFound() : Ok(result);
    }

    private static Dictionary<string, string[]> ValidateNpc(
        string? name,
        short level,
        string? npcTypeName,
        string? npcRaceName,
        string? npcSexName)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(name) || name.Length > 100)
            errors["name"] = ["Name must contain between 1 and 100 characters."];
        if (level is < 1 or > 255)
            errors["level"] = ["Level must be between 1 and 255."];
        if (string.IsNullOrEmpty(npcTypeName) || npcTypeName.Length > 64)
            errors["npcTypeName"] = ["Type must contain between 1 and 64 characters."];
        if (npcRaceName?.Length > 64)
            errors["npcRaceName"] = ["Race must not exceed 64 characters."];
        if (string.IsNullOrEmpty(npcSexName) || npcSexName.Length > 64)
            errors["npcSexName"] = ["Sex must contain between 1 and 64 characters."];
        return errors;
    }

    private static string? TrimOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

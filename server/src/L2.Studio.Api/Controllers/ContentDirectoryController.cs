using L2.Studio.Api.Filters;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
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
            ItemMaterialName = TrimOptional(request.ItemMaterialName), ItemCrystalTypeName = TrimOptional(request.ItemCrystalTypeName),
            HandlerName = TrimOptional(request.HandlerName)
        }, token);

    [HttpGet("items/{id:int}")]
    public async Task<ActionResult<L2.Studio.Contracts.ItemDetailSummary>> GetItem(string gameVersion, int id, CancellationToken token)
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
        if (request.AttackGeometry is { Radius: < 0 } || request.AttackGeometry is { Length: < 0 })
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["attackGeometry"] = ["Attack geometry radius and length must not be negative."]
            }));
        try
        {
            var item = await repository.UpdateItemAsync(gameVersion, id, request with
            {
                Name = request.Name.Trim(),
                ItemTypeName = request.ItemTypeName.Trim(),
                ItemActionName = TrimOptional(request.ItemActionName),
                ItemBodyPartName = TrimOptional(request.ItemBodyPartName),
                ItemMaterialName = TrimOptional(request.ItemMaterialName),
                ItemCrystalTypeName = TrimOptional(request.ItemCrystalTypeName),
                HandlerName = TrimOptional(request.HandlerName)
            }, token);
            return item is null ? NotFound() : Ok(item);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["definition"] = [exception.Message] }));
        }
    }

    [HttpPut("items/{itemId:int}/primary-skill")]
    public async Task<ActionResult<L2.Studio.Contracts.ItemPrimarySkillSummary>> SetItemPrimarySkill(
        string gameVersion,
        int itemId,
        SetItemPrimarySkillRequest request,
        CancellationToken token)
    {
        if (request.SkillId <= 0 || request.SkillLevel is < 1 or > 255)
            return BadRequest(ItemSkillValidationProblem());
        try
        {
            var skill = await repository.SetItemPrimarySkillAsync(gameVersion, itemId, request, token);
            return skill is null ? NotFound() : Ok(skill);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["primarySkill"] = [exception.Message]
            }));
        }
    }

    [HttpDelete("items/{itemId:int}/primary-skill")]
    public async Task<IActionResult> ClearItemPrimarySkill(string gameVersion, int itemId, CancellationToken token) =>
        await repository.ClearItemPrimarySkillAsync(gameVersion, itemId, token) ? new NoContentResult() : new NotFoundResult();

    [HttpPost("items/{itemId:int}/skills")]
    public async Task<ActionResult<L2.Studio.Contracts.ItemSkillSummary>> CreateItemSkill(
        string gameVersion,
        int itemId,
        CreateItemSkillRequest request,
        CancellationToken token)
    {
        var itemSkillTypeName = TrimOptional(request.ItemSkillTypeName);
        if (!ValidItemSkillRequest(request.SkillId, request.SkillLevel, itemSkillTypeName, request.Chance))
            return BadRequest(ItemSkillValidationProblem());
        try
        {
            var skill = await repository.CreateItemSkillAsync(gameVersion, itemId, request with
            {
                ItemSkillTypeName = itemSkillTypeName
            }, token);
            return skill is null ? NotFound() : CreatedAtAction(nameof(GetItem), new { gameVersion, id = itemId }, skill);
        }
        catch (ItemSkillConflictException exception)
        {
            return Conflict(new ProblemDetails { Title = "Item skill already exists", Detail = exception.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["skill"] = [exception.Message]
            }));
        }
    }

    [HttpPatch("items/{itemId:int}/skills/{skillId:int}/{skillLevel:int}")]
    public async Task<ActionResult<L2.Studio.Contracts.ItemSkillSummary>> UpdateItemSkill(
        string gameVersion,
        int itemId,
        int skillId,
        short skillLevel,
        UpdateItemSkillRequest request,
        CancellationToken token)
    {
        var itemSkillTypeName = TrimOptional(request.ItemSkillTypeName);
        if (!ValidItemSkillRequest(skillId, skillLevel, itemSkillTypeName, request.Chance))
            return BadRequest(ItemSkillValidationProblem());
        try
        {
            var skill = await repository.UpdateItemSkillAsync(gameVersion, itemId, skillId, skillLevel, request with
            {
                ItemSkillTypeName = itemSkillTypeName
            }, token);
            return skill is null ? NotFound() : Ok(skill);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["itemSkillTypeName"] = [exception.Message]
            }));
        }
    }

    [HttpDelete("items/{itemId:int}/skills/{skillId:int}/{skillLevel:int}")]
    public async Task<IActionResult> DeleteItemSkill(
        string gameVersion,
        int itemId,
        int skillId,
        short skillLevel,
        CancellationToken token) =>
        await repository.DeleteItemSkillAsync(gameVersion, itemId, skillId, skillLevel, token) ? new NoContentResult() : new NotFoundResult();

    [HttpDelete("items/{id:int}")]
    public Task<IActionResult> DeleteItem(string gameVersion, int id, CancellationToken token) =>
        Delete(() => repository.DeleteItemAsync(gameVersion, id, token));

    [HttpGet("item-types"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.ItemTypeSummary>> GetItemTypes(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchItemTypesAsync(gameVersion, Normalized(request), token);
    [HttpPatch("item-types/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemType(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-types", name, request, token);
    [HttpDelete("item-types/{name}")]
    public Task<IActionResult> DeleteItemType(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteItemLookupAsync(gameVersion, "item-types", name, token));
    [HttpGet("item-actions"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.ItemLookupSummary>> GetItemActions(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchItemLookupsAsync(gameVersion, "item-actions", Normalized(request), token);
    [HttpPatch("item-actions/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemAction(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-actions", name, request, token);
    [HttpDelete("item-actions/{name}")]
    public Task<IActionResult> DeleteItemAction(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteItemLookupAsync(gameVersion, "item-actions", name, token));
    [HttpGet("item-body-parts"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.ItemLookupSummary>> GetItemBodyParts(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchItemLookupsAsync(gameVersion, "item-body-parts", Normalized(request), token);
    [HttpPatch("item-body-parts/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemBodyPart(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-body-parts", name, request, token);
    [HttpDelete("item-body-parts/{name}")]
    public Task<IActionResult> DeleteItemBodyPart(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteItemLookupAsync(gameVersion, "item-body-parts", name, token));
    [HttpGet("item-materials"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.ItemLookupSummary>> GetItemMaterials(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchItemLookupsAsync(gameVersion, "item-materials", Normalized(request), token);
    [HttpPatch("item-materials/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemMaterial(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-materials", name, request, token);
    [HttpDelete("item-materials/{name}")]
    public Task<IActionResult> DeleteItemMaterial(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteItemLookupAsync(gameVersion, "item-materials", name, token));
    [HttpGet("item-crystal-types"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.ItemLookupSummary>> GetItemCrystalTypes(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchItemLookupsAsync(gameVersion, "item-crystal-types", Normalized(request), token);
    [HttpPatch("item-crystal-types/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemCrystalType(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-crystal-types", name, request, token);
    [HttpDelete("item-crystal-types/{name}")]
    public Task<IActionResult> DeleteItemCrystalType(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteItemLookupAsync(gameVersion, "item-crystal-types", name, token));
    [HttpGet("item-handlers"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.ItemLookupSummary>> GetItemHandlers(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchItemLookupsAsync(gameVersion, "item-handlers", Normalized(request), token);
    [HttpPatch("item-handlers/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemHandler(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-handlers", name, request, token);
    [HttpDelete("item-handlers/{name}")]
    public Task<IActionResult> DeleteItemHandler(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteItemLookupAsync(gameVersion, "item-handlers", name, token));
    [HttpGet("item-skill-types"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.ItemLookupSummary>> GetItemSkillTypes(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchItemLookupsAsync(gameVersion, "item-skill-types", Normalized(request), token);
    [HttpPatch("item-skill-types/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.ItemLookupSummary>> UpdateItemSkillType(string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) => UpdateItemLookup(gameVersion, "item-skill-types", name, request, token);
    [HttpDelete("item-skill-types/{name}")]
    public Task<IActionResult> DeleteItemSkillType(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteItemLookupAsync(gameVersion, "item-skill-types", name, token));
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
    [HttpDelete("npcs/{id:int}")]
    public Task<IActionResult> DeleteNpc(string gameVersion, int id, CancellationToken token) =>
        Delete(() => repository.DeleteNpcAsync(gameVersion, id, token));

    [HttpGet("skills"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.SkillDirectoryPage> SearchSkills(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchSkillsAsync(gameVersion, request.Query?.Trim() ?? string.Empty, request.Page, request.PageSize, token);
    [HttpPatch("skills/{id:int}")]
    public async Task<ActionResult<L2.Studio.Contracts.SkillSummary>> UpdateSkill(string gameVersion, int id, UpdateSkillRequest request, CancellationToken token)
    {
        var name = request.Name?.Trim();
        var operateTypeName = TrimOptional(request.SkillOperateTypeName);
        var targetTypeName = TrimOptional(request.SkillTargetTypeName);
        if (string.IsNullOrEmpty(name) || name.Length > 100 || request.Levels is < 1 or > 255)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["definition"] = ["Skill name must contain between 1 and 100 characters and levels must be between 1 and 255."]
            }));
        try
        {
            var skill = await repository.UpdateSkillAsync(gameVersion, id, request with
            {
                Name = name,
                SkillOperateTypeName = operateTypeName,
                SkillTargetTypeName = targetTypeName
            }, token);
            return skill is null ? NotFound() : Ok(skill);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["definition"] = [exception.Message] }));
        }
    }
    [HttpDelete("skills/{id:int}")]
    public Task<IActionResult> DeleteSkill(string gameVersion, int id, CancellationToken token) =>
        Delete(() => repository.DeleteSkillAsync(gameVersion, id, token));

    [HttpGet("npc-types"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.NpcLookupSummary>> GetNpcTypes(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchNpcLookupsAsync(gameVersion, "npc-types", Normalized(request), token);
    [HttpPatch("npc-types/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.NpcLookupSummary>> UpdateNpcType(
        string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) =>
        UpdateNpcLookup(gameVersion, "npc-types", name, request, token);
    [HttpDelete("npc-types/{name}")]
    public Task<IActionResult> DeleteNpcType(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteNpcLookupAsync(gameVersion, "npc-types", name, token));
    [HttpGet("npc-races"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.NpcLookupSummary>> GetNpcRaces(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchNpcLookupsAsync(gameVersion, "npc-races", Normalized(request), token);
    [HttpPatch("npc-races/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.NpcLookupSummary>> UpdateNpcRace(
        string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) =>
        UpdateNpcLookup(gameVersion, "npc-races", name, request, token);
    [HttpDelete("npc-races/{name}")]
    public Task<IActionResult> DeleteNpcRace(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteNpcLookupAsync(gameVersion, "npc-races", name, token));
    [HttpGet("npc-sexes"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.NpcLookupSummary>> GetNpcSexes(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchNpcLookupsAsync(gameVersion, "npc-sexes", Normalized(request), token);
    [HttpPatch("npc-sexes/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.NpcLookupSummary>> UpdateNpcSex(
        string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) =>
        UpdateNpcLookup(gameVersion, "npc-sexes", name, request, token);
    [HttpDelete("npc-sexes/{name}")]
    public Task<IActionResult> DeleteNpcSex(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteNpcLookupAsync(gameVersion, "npc-sexes", name, token));
    [HttpGet("player-classes")]
    public Task<IReadOnlyList<L2.Studio.Contracts.PlayerClassSummary>> GetPlayerClasses(string gameVersion, CancellationToken token) => repository.GetPlayerClassesAsync(gameVersion, token);
    [HttpPatch("player-classes/{id:int}")]
    public async Task<ActionResult<L2.Studio.Contracts.PlayerClassSummary>> UpdatePlayerClass(string gameVersion, int id, UpdatePlayerClassRequest request, CancellationToken token)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 64 || request.ParentClassId == id)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["definition"] = ["Name must contain between 1 and 64 characters and a class cannot be its own parent."] }));
        try
        {
            var result = await repository.UpdatePlayerClassAsync(gameVersion, id, request with { Name = name }, token);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["definition"] = [exception.Message] }));
        }
    }
    [HttpDelete("player-classes/{id:int}")]
    public Task<IActionResult> DeletePlayerClass(string gameVersion, int id, CancellationToken token) => Delete(() => repository.DeletePlayerClassAsync(gameVersion, id, token));
    [HttpGet("player-races"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.PlayerLookupSummary>> GetPlayerRaces(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchPlayerLookupsAsync(gameVersion, "player-races", Normalized(request), token);
    [HttpPatch("player-races/{id:int}")]
    public Task<ActionResult<L2.Studio.Contracts.PlayerLookupSummary>> UpdatePlayerRace(string gameVersion, int id, UpdatePlayerNameRequest request, CancellationToken token) =>
        UpdatePlayerLookup(gameVersion, "player-races", id, request, token);
    [HttpDelete("player-races/{id:int}")]
    public Task<IActionResult> DeletePlayerRace(string gameVersion, int id, CancellationToken token) => Delete(() => repository.DeletePlayerLookupAsync(gameVersion, "player-races", id, token));
    [HttpGet("player-sexes"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.PlayerLookupSummary>> GetPlayerSexes(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchPlayerLookupsAsync(gameVersion, "player-sexes", Normalized(request), token);
    [HttpPatch("player-sexes/{id:int}")]
    public Task<ActionResult<L2.Studio.Contracts.PlayerLookupSummary>> UpdatePlayerSex(string gameVersion, int id, UpdatePlayerNameRequest request, CancellationToken token) =>
        UpdatePlayerLookup(gameVersion, "player-sexes", id, request, token);
    [HttpDelete("player-sexes/{id:int}")]
    public Task<IActionResult> DeletePlayerSex(string gameVersion, int id, CancellationToken token) => Delete(() => repository.DeletePlayerLookupAsync(gameVersion, "player-sexes", id, token));
    [HttpGet("player-faces"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.PlayerAppearanceSummary>> GetPlayerFaces(string gameVersion, [FromQuery] PlayerAppearanceDirectoryRequest request, CancellationToken token) =>
        repository.SearchPlayerAppearancesAsync(gameVersion, "player-faces", Normalized(request), token);
    [HttpPatch("player-faces/{id:int}/races/{playerRaceId:int}/sexes/{playerSexId:int}")]
    public Task<ActionResult<L2.Studio.Contracts.PlayerAppearanceSummary>> UpdatePlayerFace(string gameVersion, int id, int playerRaceId, int playerSexId, UpdatePlayerNameRequest request, CancellationToken token) =>
        UpdatePlayerAppearance(gameVersion, "player-faces", id, playerRaceId, playerSexId, request, token);
    [HttpDelete("player-faces/{id:int}/races/{playerRaceId:int}/sexes/{playerSexId:int}")]
    public Task<IActionResult> DeletePlayerFace(string gameVersion, int id, int playerRaceId, int playerSexId, CancellationToken token) => Delete(() => repository.DeletePlayerAppearanceAsync(gameVersion, "player-faces", id, playerRaceId, playerSexId, token));
    [HttpGet("player-hair-styles"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.PlayerAppearanceSummary>> GetPlayerHairStyles(string gameVersion, [FromQuery] PlayerAppearanceDirectoryRequest request, CancellationToken token) =>
        repository.SearchPlayerAppearancesAsync(gameVersion, "player-hair-styles", Normalized(request), token);
    [HttpPatch("player-hair-styles/{id:int}/races/{playerRaceId:int}/sexes/{playerSexId:int}")]
    public Task<ActionResult<L2.Studio.Contracts.PlayerAppearanceSummary>> UpdatePlayerHairStyle(string gameVersion, int id, int playerRaceId, int playerSexId, UpdatePlayerNameRequest request, CancellationToken token) =>
        UpdatePlayerAppearance(gameVersion, "player-hair-styles", id, playerRaceId, playerSexId, request, token);
    [HttpDelete("player-hair-styles/{id:int}/races/{playerRaceId:int}/sexes/{playerSexId:int}")]
    public Task<IActionResult> DeletePlayerHairStyle(string gameVersion, int id, int playerRaceId, int playerSexId, CancellationToken token) => Delete(() => repository.DeletePlayerAppearanceAsync(gameVersion, "player-hair-styles", id, playerRaceId, playerSexId, token));
    [HttpGet("player-hair-colors"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.PlayerAppearanceSummary>> GetPlayerHairColors(string gameVersion, [FromQuery] PlayerAppearanceDirectoryRequest request, CancellationToken token) =>
        repository.SearchPlayerAppearancesAsync(gameVersion, "player-hair-colors", Normalized(request), token);
    [HttpPatch("player-hair-colors/{id:int}/races/{playerRaceId:int}/sexes/{playerSexId:int}")]
    public Task<ActionResult<L2.Studio.Contracts.PlayerAppearanceSummary>> UpdatePlayerHairColor(string gameVersion, int id, int playerRaceId, int playerSexId, UpdatePlayerNameRequest request, CancellationToken token) =>
        UpdatePlayerAppearance(gameVersion, "player-hair-colors", id, playerRaceId, playerSexId, request, token);
    [HttpDelete("player-hair-colors/{id:int}/races/{playerRaceId:int}/sexes/{playerSexId:int}")]
    public Task<IActionResult> DeletePlayerHairColor(string gameVersion, int id, int playerRaceId, int playerSexId, CancellationToken token) => Delete(() => repository.DeletePlayerAppearanceAsync(gameVersion, "player-hair-colors", id, playerRaceId, playerSexId, token));
    [HttpGet("skill-operate-types"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.SkillLookupSummary>> GetSkillOperateTypes(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchSkillLookupsAsync(gameVersion, "skill-operate-types", Normalized(request), token);
    [HttpPatch("skill-operate-types/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.SkillLookupSummary>> UpdateSkillOperateType(
        string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) =>
        UpdateSkillLookup(gameVersion, "skill-operate-types", name, request, token);
    [HttpDelete("skill-operate-types/{name}")]
    public Task<IActionResult> DeleteSkillOperateType(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteSkillLookupAsync(gameVersion, "skill-operate-types", name, token));
    [HttpGet("skill-target-types"), ValidateDirectoryRequest]
    public Task<L2.Studio.Contracts.DirectoryPage<L2.Studio.Contracts.SkillLookupSummary>> GetSkillTargetTypes(string gameVersion, [FromQuery] DirectoryRequest request, CancellationToken token) =>
        repository.SearchSkillLookupsAsync(gameVersion, "skill-target-types", Normalized(request), token);
    [HttpPatch("skill-target-types/{name}")]
    public Task<ActionResult<L2.Studio.Contracts.SkillLookupSummary>> UpdateSkillTargetType(
        string gameVersion, string name, UpdateNpcLookupRequest request, CancellationToken token) =>
        UpdateSkillLookup(gameVersion, "skill-target-types", name, request, token);
    [HttpDelete("skill-target-types/{name}")]
    public Task<IActionResult> DeleteSkillTargetType(string gameVersion, string name, CancellationToken token) => Delete(() => repository.DeleteSkillLookupAsync(gameVersion, "skill-target-types", name, token));

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

    private async Task<ActionResult<L2.Studio.Contracts.SkillLookupSummary>> UpdateSkillLookup(
        string gameVersion, string kind, string name, UpdateNpcLookupRequest request, CancellationToken token)
    {
        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrEmpty(displayName) || displayName.Length > 64)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["displayName"] = ["Display name must contain between 1 and 64 characters."] }));
        var result = await repository.UpdateSkillLookupDisplayNameAsync(gameVersion, kind, name, displayName, token);
        return result is null ? NotFound() : Ok(result);
    }

    private async Task<ActionResult<L2.Studio.Contracts.PlayerLookupSummary>> UpdatePlayerLookup(
        string gameVersion, string kind, int id, UpdatePlayerNameRequest request, CancellationToken token)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 64)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["name"] = ["Name must contain between 1 and 64 characters."] }));
        var result = await repository.UpdatePlayerLookupNameAsync(gameVersion, kind, id, name, token);
        return result is null ? NotFound() : Ok(result);
    }

    private async Task<ActionResult<L2.Studio.Contracts.PlayerAppearanceSummary>> UpdatePlayerAppearance(
        string gameVersion, string kind, int id, int playerRaceId, int playerSexId, UpdatePlayerNameRequest request, CancellationToken token)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 64)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["name"] = ["Name must contain between 1 and 64 characters."] }));
        var result = await repository.UpdatePlayerAppearanceNameAsync(gameVersion, kind, id, playerRaceId, playerSexId, name, token);
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

    private static bool ValidItemSkillRequest(
        int skillId,
        short skillLevel,
        string? itemSkillTypeName,
        int? chance) =>
        skillId > 0 &&
        skillLevel is >= 1 and <= 255 &&
        (itemSkillTypeName is null || itemSkillTypeName.Length <= 64) &&
        (chance is null || chance is >= 0 and <= 100);

    private static ValidationProblemDetails ItemSkillValidationProblem() => new(new Dictionary<string, string[]>
    {
        ["itemSkill"] = ["Skill ID and level are required, the type must not exceed 64 characters, and chance must be between 0 and 100."]
    });

    private static string? TrimOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DirectoryRequest Normalized(DirectoryRequest request) =>
        request with { Query = request.Query?.Trim() };

    private static PlayerAppearanceDirectoryRequest Normalized(PlayerAppearanceDirectoryRequest request) =>
        request with { Query = request.Query?.Trim() };

    private static async Task<IActionResult> Delete(Func<Task<bool>> delete)
    {
        try
        {
            return await delete() ? new NoContentResult() : new NotFoundResult();
        }
        catch (ContentDeleteConflictException exception)
        {
            return new ConflictObjectResult(new ProblemDetails
            {
                Title = "Record is in use",
                Detail = $"This record is used by {exception.DependentCount} {exception.DependentType}.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}

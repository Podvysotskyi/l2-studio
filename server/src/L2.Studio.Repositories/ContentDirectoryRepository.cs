using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Context.Identifiers;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Repositories;

public sealed partial class ContentDirectoryRepository(
    IDbContextFactory<GameContentDbContext> contextFactory)
    : IContentDirectoryRepository
{
    public async Task<NpcDirectoryPage> SearchNpcsAsync(
        string gameVersion,
        NpcDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.Query ?? string.Empty;
        var offset = ((long)request.Page - 1) * request.PageSize;
        var searchPattern = $"%{EscapeLikePattern(query)}%";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var npcs = context.Npcs
            .AsNoTracking()
            .Where(npc => npc.GameVersion == gameVersion && (query == string.Empty ||
                (npc.Name != null && EF.Functions.ILike(npc.Name, searchPattern, "\\"))));
        if (request.NpcTypeName is not null)
            npcs = npcs.Where(npc => npc.NpcTypeName == request.NpcTypeName);
        if (request.NpcRaceName is not null)
            npcs = npcs.Where(npc => npc.NpcRaceName == request.NpcRaceName);
        if (request.WithoutRace is true)
            npcs = npcs.Where(npc => npc.NpcRaceName == null);
        if (request.NpcSexName is not null)
            npcs = npcs.Where(npc => npc.NpcSexName == request.NpcSexName);
        var visualNpcIds = await NpcVisualIdsAsync(context, gameVersion, cancellationToken);
        if (request.HasVisuals is true)
            npcs = npcs.Where(npc => visualNpcIds.Contains(npc.Id));
        if (request.HasVisuals is false)
            npcs = npcs.Where(npc => !visualNpcIds.Contains(npc.Id));
        var total = await npcs.LongCountAsync(cancellationToken);
        if (offset > int.MaxValue)
        {
            return new NpcDirectoryPage([], total, request.Page, request.PageSize);
        }

        var items = await ProjectNpcs(npcs
            .OrderBy(npc => npc.Id)
            .Skip((int)offset)
            .Take(request.PageSize))
            .ToListAsync(cancellationToken);

        return new NpcDirectoryPage(WithVisuals(items, visualNpcIds), total, request.Page, request.PageSize);
    }

    public async Task<NpcSummary?> GetNpcAsync(
        string gameVersion,
        int id,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var visualNpcIds = await NpcVisualIdsAsync(context, gameVersion, cancellationToken);
        var npc = await ProjectNpcs(context.Npcs.AsNoTracking().Where(npc =>
                npc.GameVersion == gameVersion && npc.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        return npc is null ? null : WithVisuals(npc, visualNpcIds);
    }

    public async Task<NpcSummary?> UpdateNpcAsync(
        string gameVersion,
        int id,
        string name,
        short level,
        string npcTypeName,
        string? npcRaceName,
        string npcSexName,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var npc = await context.Npcs.SingleOrDefaultAsync(value =>
            value.GameVersion == gameVersion && value.Id == id, cancellationToken);
        if (npc is null) return null;

        if (!await context.NpcTypes.AnyAsync(value =>
                value.GameVersion == gameVersion && value.Name == npcTypeName, cancellationToken))
            throw new InvalidOperationException("NPC type is not available for this game version.");
        if (npcRaceName is not null && !await context.NpcRaces.AnyAsync(value =>
                value.GameVersion == gameVersion && value.Name == npcRaceName, cancellationToken))
            throw new InvalidOperationException("NPC race is not available for this game version.");
        if (!await context.NpcSexes.AnyAsync(value =>
                value.GameVersion == gameVersion && value.Name == npcSexName, cancellationToken))
            throw new InvalidOperationException("NPC sex is not available for this game version.");

        npc.Name = name;
        npc.Level = level;
        npc.NpcTypeName = npcTypeName;
        npc.NpcRaceName = npcRaceName;
        npc.NpcSexName = npcSexName;
        await context.SaveChangesAsync(cancellationToken);
        var visualNpcIds = await NpcVisualIdsAsync(context, gameVersion, cancellationToken);
        var updated = await ProjectNpcs(context.Npcs.AsNoTracking().Where(value =>
                value.GameVersion == gameVersion && value.Id == id))
            .SingleAsync(cancellationToken);
        return WithVisuals(updated, visualNpcIds);
    }

    public async Task<bool> DeleteNpcAsync(string gameVersion, int id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var npc = await context.Npcs.SingleOrDefaultAsync(value =>
            value.GameVersion == gameVersion && value.Id == id, cancellationToken);
        if (npc is null) return false;
        context.Npcs.Remove(npc);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DirectoryPage<NpcLookupSummary>> SearchNpcLookupsAsync(
        string gameVersion,
        string kind,
        DirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.Query ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return kind switch
        {
            "npc-types" => await PageAsync(context.NpcTypes.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion && (query == string.Empty ||
                    EF.Functions.ILike(item.Name, pattern, "\\") || EF.Functions.ILike(item.DisplayName, pattern, "\\")))
                .OrderBy(item => item.Name)
                .Select(item => new NpcLookupSummary(item.Name, item.DisplayName)), request.Page, request.PageSize, cancellationToken),
            "npc-races" => await PageAsync(context.NpcRaces.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion && (query == string.Empty ||
                    EF.Functions.ILike(item.Name, pattern, "\\") || EF.Functions.ILike(item.DisplayName, pattern, "\\")))
                .OrderBy(item => item.Name)
                .Select(item => new NpcLookupSummary(item.Name, item.DisplayName)), request.Page, request.PageSize, cancellationToken),
            "npc-sexes" => await PageAsync(context.NpcSexes.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion && (query == string.Empty ||
                    EF.Functions.ILike(item.Name, pattern, "\\") || EF.Functions.ILike(item.DisplayName, pattern, "\\")))
                .OrderBy(item => item.Name)
                .Select(item => new NpcLookupSummary(item.Name, item.DisplayName)), request.Page, request.PageSize, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public async Task<NpcLookupSummary?> UpdateNpcLookupDisplayNameAsync(
        string gameVersion,
        string kind,
        string name,
        string displayName,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        switch (kind)
        {
            case "npc-types":
            {
                var item = await context.NpcTypes.SingleOrDefaultAsync(
                    value => value.GameVersion == gameVersion && value.Name == name, cancellationToken);
                if (item is null) return null;
                item.DisplayName = displayName;
                await context.SaveChangesAsync(cancellationToken);
                return new NpcLookupSummary(item.Name, item.DisplayName);
            }
            case "npc-races":
            {
                var item = await context.NpcRaces.SingleOrDefaultAsync(
                    value => value.GameVersion == gameVersion && value.Name == name, cancellationToken);
                if (item is null) return null;
                item.DisplayName = displayName;
                await context.SaveChangesAsync(cancellationToken);
                return new NpcLookupSummary(item.Name, item.DisplayName);
            }
            case "npc-sexes":
            {
                var item = await context.NpcSexes.SingleOrDefaultAsync(
                    value => value.GameVersion == gameVersion && value.Name == name, cancellationToken);
                if (item is null) return null;
                item.DisplayName = displayName;
                await context.SaveChangesAsync(cancellationToken);
                return new NpcLookupSummary(item.Name, item.DisplayName);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public async Task<bool> DeleteNpcLookupAsync(
        string gameVersion,
        string kind,
        string name,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var usageCount = kind switch
        {
            "npc-types" => await context.Npcs.CountAsync(item => item.GameVersion == gameVersion && item.NpcTypeName == name, cancellationToken),
            "npc-races" => await context.Npcs.CountAsync(item => item.GameVersion == gameVersion && item.NpcRaceName == name, cancellationToken),
            "npc-sexes" => await context.Npcs.CountAsync(item => item.GameVersion == gameVersion && item.NpcSexName == name, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (usageCount > 0) throw new ContentDeleteConflictException("NPC definitions", usageCount);

        var deleted = kind switch
        {
            "npc-types" => await DeleteLookupAsync(context.NpcTypes, gameVersion, name, cancellationToken),
            "npc-races" => await DeleteLookupAsync(context.NpcRaces, gameVersion, name, cancellationToken),
            "npc-sexes" => await DeleteLookupAsync(context.NpcSexes, gameVersion, name, cancellationToken),
            _ => false
        };
        if (!deleted) return false;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PlayerClassSummary>> GetPlayerClassesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var variants = await context.PlayerClasses
            .AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .OrderBy(item => item.Id)
            .ThenBy(item => item.PlayerRaceId)
            .ThenBy(item => item.PlayerSexId)
            .Select(item => new
            {
                Id = (int)item.Id,
                item.Name,
                ParentClassId = (int?)item.ParentClassId,
                item.IsMage,
                RaceId = (int)item.PlayerRaceId,
                RaceName = item.PlayerRace.Name,
                SexId = (int)item.PlayerSexId,
                SexName = item.PlayerSex.Name
            })
            .ToListAsync(cancellationToken);

        return variants
            .GroupBy(item => new { item.Id, item.Name, item.ParentClassId, item.IsMage })
            .OrderBy(group => group.Key.Id)
            .Select(group => new PlayerClassSummary(
                group.Key.Id,
                group.Key.Name,
                group.Key.ParentClassId,
                group.Key.IsMage,
                group.GroupBy(item => new { item.RaceId, item.RaceName })
                    .OrderBy(race => race.Key.RaceId)
                    .Select(race => new PlayerClassRaceSummary(
                        race.Key.RaceId,
                        race.Key.RaceName,
                        race.OrderBy(item => item.SexId)
                            .Select(item => new PlayerSexSummary(item.SexId, item.SexName))
                            .ToArray()))
                    .ToArray()))
            .ToArray();
    }

    public async Task<PlayerClassSummary?> UpdatePlayerClassAsync(
        string gameVersion, int id, UpdatePlayerClassRequest request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var classId = (PlayerClassId)id;
        var variants = await context.PlayerClasses.Where(value => value.GameVersion == gameVersion && value.Id == classId)
            .ToListAsync(cancellationToken);
        if (variants.Count == 0) return null;
        if (request.ParentClassId is not null)
        {
            var parentClassId = (PlayerClassId)request.ParentClassId.Value;
            foreach (var variant in variants)
            {
                var parentExists = await context.PlayerClasses.AnyAsync(value =>
                    value.GameVersion == gameVersion && value.Id == parentClassId && value.PlayerRaceId == variant.PlayerRaceId && value.PlayerSexId == variant.PlayerSexId,
                    cancellationToken);
                if (!parentExists)
                    throw new InvalidOperationException("The selected parent class is not available for every race and sex variant.");
            }
        }
        foreach (var variant in variants)
        {
            variant.Name = request.Name!.Trim();
            variant.IsMage = request.IsMage;
            variant.ParentClassId = request.ParentClassId is null ? null : (PlayerClassId)request.ParentClassId.Value;
        }
        await context.SaveChangesAsync(cancellationToken);
        return (await GetPlayerClassesAsync(gameVersion, cancellationToken)).Single(value => value.Id == id);
    }

    public async Task<bool> DeletePlayerClassAsync(string gameVersion, int id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var classId = (PlayerClassId)id;
        var variants = await context.PlayerClasses.Where(value => value.GameVersion == gameVersion && value.Id == classId)
            .ToListAsync(cancellationToken);
        if (variants.Count == 0) return false;
        var dependentCount = await context.PlayerClasses.CountAsync(value =>
            value.GameVersion == gameVersion && value.ParentClassId == classId && value.Id != classId, cancellationToken);
        if (dependentCount > 0) throw new ContentDeleteConflictException("child player classes", dependentCount);
        context.PlayerClasses.RemoveRange(variants);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DirectoryPage<PlayerLookupSummary>> SearchPlayerLookupsAsync(
        string gameVersion,
        string kind,
        DirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.Query ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        var matchingId = int.TryParse(query, out var parsedId) ? parsedId : (int?)null;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return kind switch
        {
            "player-races" => await PageAsync(context.PlayerRaces.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion && (query == string.Empty || (int)item.Id == matchingId ||
                    EF.Functions.ILike(item.Name, pattern, "\\")))
                .OrderBy(item => item.Id)
                .Select(item => new PlayerLookupSummary((int)item.Id, item.Name)), request.Page, request.PageSize, cancellationToken),
            "player-sexes" => await PageAsync(context.PlayerSexes.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion && (query == string.Empty || (int)item.Id == matchingId ||
                    EF.Functions.ILike(item.Name, pattern, "\\")))
                .OrderBy(item => item.Id)
                .Select(item => new PlayerLookupSummary((int)item.Id, item.Name)), request.Page, request.PageSize, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public async Task<PlayerLookupSummary?> UpdatePlayerLookupNameAsync(
        string gameVersion, string kind, int id, string name, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        switch (kind)
        {
            case "player-races":
            {
                var item = await context.PlayerRaces.SingleOrDefaultAsync(value => value.GameVersion == gameVersion && (int)value.Id == id, cancellationToken);
                if (item is null) return null;
                item.Name = name;
                await context.SaveChangesAsync(cancellationToken);
                return new PlayerLookupSummary((int)item.Id, item.Name);
            }
            case "player-sexes":
            {
                var item = await context.PlayerSexes.SingleOrDefaultAsync(value => value.GameVersion == gameVersion && (int)value.Id == id, cancellationToken);
                if (item is null) return null;
                item.Name = name;
                await context.SaveChangesAsync(cancellationToken);
                return new PlayerLookupSummary((int)item.Id, item.Name);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public async Task<bool> DeletePlayerLookupAsync(
        string gameVersion, string kind, int id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var usages = kind switch
        {
            "player-races" => await context.PlayerClasses.CountAsync(value => value.GameVersion == gameVersion && (int)value.PlayerRaceId == id, cancellationToken) +
                await context.PlayerFaces.CountAsync(value => value.GameVersion == gameVersion && (int)value.PlayerRaceId == id, cancellationToken) +
                await context.PlayerHairStyles.CountAsync(value => value.GameVersion == gameVersion && (int)value.PlayerRaceId == id, cancellationToken) +
                await context.PlayerHairColors.CountAsync(value => value.GameVersion == gameVersion && (int)value.PlayerRaceId == id, cancellationToken),
            "player-sexes" => await context.PlayerClasses.CountAsync(value => value.GameVersion == gameVersion && (int)value.PlayerSexId == id, cancellationToken) +
                await context.PlayerFaces.CountAsync(value => value.GameVersion == gameVersion && (int)value.PlayerSexId == id, cancellationToken) +
                await context.PlayerHairStyles.CountAsync(value => value.GameVersion == gameVersion && (int)value.PlayerSexId == id, cancellationToken) +
                await context.PlayerHairColors.CountAsync(value => value.GameVersion == gameVersion && (int)value.PlayerSexId == id, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (usages > 0) throw new ContentDeleteConflictException("player classes or appearance options", usages);
        var deleted = kind switch
        {
            "player-races" => await DeletePlayerLookupAsync(context.PlayerRaces, gameVersion, (PlayerRaceId)id, cancellationToken),
            "player-sexes" => await DeletePlayerLookupAsync(context.PlayerSexes, gameVersion, (PlayerSexId)id, cancellationToken),
            _ => false
        };
        if (!deleted) return false;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DirectoryPage<PlayerAppearanceSummary>> SearchPlayerAppearancesAsync(
        string gameVersion,
        string kind,
        PlayerAppearanceDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.Query ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return kind switch
        {
            "player-faces" => await PageAsync(context.PlayerFaces.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion &&
                    (query == string.Empty || EF.Functions.ILike(item.Name, pattern, "\\") || EF.Functions.ILike(item.PlayerRace.Name, pattern, "\\") || EF.Functions.ILike(item.PlayerSex.Name, pattern, "\\")) &&
                    (request.PlayerRaceId == null || (int)item.PlayerRaceId == request.PlayerRaceId) &&
                    (request.PlayerSexId == null || (int)item.PlayerSexId == request.PlayerSexId))
                .OrderBy(item => item.PlayerRaceId).ThenBy(item => item.PlayerSexId).ThenBy(item => item.Id)
                .Select(item => new PlayerAppearanceSummary(item.Id, item.Name, (int)item.PlayerRaceId, item.PlayerRace.Name, (int)item.PlayerSexId, item.PlayerSex.Name)), request.Page, request.PageSize, cancellationToken),
            "player-hair-styles" => await PageAsync(context.PlayerHairStyles.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion &&
                    (query == string.Empty || EF.Functions.ILike(item.Name, pattern, "\\") || EF.Functions.ILike(item.PlayerRace.Name, pattern, "\\") || EF.Functions.ILike(item.PlayerSex.Name, pattern, "\\")) &&
                    (request.PlayerRaceId == null || (int)item.PlayerRaceId == request.PlayerRaceId) &&
                    (request.PlayerSexId == null || (int)item.PlayerSexId == request.PlayerSexId))
                .OrderBy(item => item.PlayerRaceId).ThenBy(item => item.PlayerSexId).ThenBy(item => item.Id)
                .Select(item => new PlayerAppearanceSummary(item.Id, item.Name, (int)item.PlayerRaceId, item.PlayerRace.Name, (int)item.PlayerSexId, item.PlayerSex.Name)), request.Page, request.PageSize, cancellationToken),
            "player-hair-colors" => await PageAsync(context.PlayerHairColors.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion &&
                    (query == string.Empty || EF.Functions.ILike(item.Name, pattern, "\\") || EF.Functions.ILike(item.PlayerRace.Name, pattern, "\\") || EF.Functions.ILike(item.PlayerSex.Name, pattern, "\\")) &&
                    (request.PlayerRaceId == null || (int)item.PlayerRaceId == request.PlayerRaceId) &&
                    (request.PlayerSexId == null || (int)item.PlayerSexId == request.PlayerSexId))
                .OrderBy(item => item.PlayerRaceId).ThenBy(item => item.PlayerSexId).ThenBy(item => item.Id)
                .Select(item => new PlayerAppearanceSummary(item.Id, item.Name, (int)item.PlayerRaceId, item.PlayerRace.Name, (int)item.PlayerSexId, item.PlayerSex.Name)), request.Page, request.PageSize, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public async Task<PlayerAppearanceSummary?> UpdatePlayerAppearanceNameAsync(
        string gameVersion, string kind, int id, int playerRaceId, int playerSexId, string name, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return kind switch
        {
            "player-faces" => await UpdatePlayerAppearanceAsync(context, context.PlayerFaces, gameVersion, id, playerRaceId, playerSexId, name, cancellationToken),
            "player-hair-styles" => await UpdatePlayerAppearanceAsync(context, context.PlayerHairStyles, gameVersion, id, playerRaceId, playerSexId, name, cancellationToken),
            "player-hair-colors" => await UpdatePlayerAppearanceAsync(context, context.PlayerHairColors, gameVersion, id, playerRaceId, playerSexId, name, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public async Task<bool> DeletePlayerAppearanceAsync(
        string gameVersion, string kind, int id, int playerRaceId, int playerSexId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var deleted = kind switch
        {
            "player-faces" => await DeletePlayerAppearanceAsync(context.PlayerFaces, gameVersion, id, playerRaceId, playerSexId, cancellationToken),
            "player-hair-styles" => await DeletePlayerAppearanceAsync(context.PlayerHairStyles, gameVersion, id, playerRaceId, playerSexId, cancellationToken),
            "player-hair-colors" => await DeletePlayerAppearanceAsync(context.PlayerHairColors, gameVersion, id, playerRaceId, playerSexId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (!deleted) return false;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<SkillDirectoryPage> SearchSkillsAsync(
        string gameVersion,
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var offset = ((long)page - 1) * pageSize;
        var searchPattern = $"%{EscapeLikePattern(query)}%";
        var hasNumericQuery = int.TryParse(query, out var skillId);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var skills = context.Skills
            .AsNoTracking()
            .Where(skill => skill.GameVersion == gameVersion &&
                (query == string.Empty || (hasNumericQuery && skill.Id == skillId) ||
                    EF.Functions.ILike(skill.Name, searchPattern, "\\")));
        var total = await skills.LongCountAsync(cancellationToken);
        if (offset > int.MaxValue)
        {
            return new SkillDirectoryPage([], total, page, pageSize);
        }

        var items = await skills
            .OrderBy(skill => skill.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .Select(skill => new SkillSummary(
                skill.Id,
                skill.Levels,
                skill.Name,
                skill.SkillOperateTypeName,
                skill.SkillOperateType == null ? null : skill.SkillOperateType.DisplayName,
                skill.SkillTargetTypeName,
                skill.SkillTargetType == null ? null : skill.SkillTargetType.DisplayName,
                skill.SkillIcons.Count))
            .ToListAsync(cancellationToken);

        return new SkillDirectoryPage(items, total, page, pageSize);
    }

    public async Task<SkillSummary?> GetSkillAsync(string gameVersion, int id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Skills.AsNoTracking()
            .Where(skill => skill.GameVersion == gameVersion && skill.Id == id)
            .Select(skill => new SkillSummary(
                skill.Id,
                skill.Levels,
                skill.Name,
                skill.SkillOperateTypeName,
                skill.SkillOperateType == null ? null : skill.SkillOperateType.DisplayName,
                skill.SkillTargetTypeName,
                skill.SkillTargetType == null ? null : skill.SkillTargetType.DisplayName,
                skill.SkillIcons.Count))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SkillSummary?> UpdateSkillAsync(
        string gameVersion,
        int id,
        UpdateSkillRequest request,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var skill = await context.Skills.SingleOrDefaultAsync(value =>
            value.GameVersion == gameVersion && value.Id == id, cancellationToken);
        if (skill is null) return null;
        if (request.SkillOperateTypeName is not null && !await context.SkillOperateTypes.AnyAsync(value =>
                value.GameVersion == gameVersion && value.Name == request.SkillOperateTypeName, cancellationToken))
            throw new InvalidOperationException("Skill operate type is not available for this game version.");
        if (request.SkillTargetTypeName is not null && !await context.SkillTargetTypes.AnyAsync(value =>
                value.GameVersion == gameVersion && value.Name == request.SkillTargetTypeName, cancellationToken))
            throw new InvalidOperationException("Skill target type is not available for this game version.");
        skill.Name = request.Name!.Trim();
        skill.Levels = request.Levels;
        skill.SkillOperateTypeName = request.SkillOperateTypeName;
        skill.SkillTargetTypeName = request.SkillTargetTypeName;
        await context.SaveChangesAsync(cancellationToken);
        return await context.Skills.AsNoTracking().Where(value => value.GameVersion == gameVersion && value.Id == id)
            .Select(value => new SkillSummary(value.Id, value.Levels, value.Name, value.SkillOperateTypeName,
                value.SkillOperateType == null ? null : value.SkillOperateType.DisplayName, value.SkillTargetTypeName,
                value.SkillTargetType == null ? null : value.SkillTargetType.DisplayName, value.SkillIcons.Count))
            .SingleAsync(cancellationToken);
    }

    public async Task<bool> DeleteSkillAsync(string gameVersion, int id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var skill = await context.Skills.SingleOrDefaultAsync(value => value.GameVersion == gameVersion && value.Id == id, cancellationToken);
        if (skill is null) return false;
        context.Skills.Remove(skill);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DirectoryPage<SkillLookupSummary>> SearchSkillLookupsAsync(
        string gameVersion,
        string kind,
        DirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.Query ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return kind switch
        {
            "skill-operate-types" => await PageAsync(context.SkillOperateTypes.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion && (query == string.Empty ||
                    EF.Functions.ILike(item.Name, pattern, "\\") || EF.Functions.ILike(item.DisplayName, pattern, "\\")))
                .OrderBy(item => item.Name)
                .Select(item => new SkillLookupSummary(item.Name, item.DisplayName)), request.Page, request.PageSize, cancellationToken),
            "skill-target-types" => await PageAsync(context.SkillTargetTypes.AsNoTracking()
                .Where(item => item.GameVersion == gameVersion && (query == string.Empty ||
                    EF.Functions.ILike(item.Name, pattern, "\\") || EF.Functions.ILike(item.DisplayName, pattern, "\\")))
                .OrderBy(item => item.Name)
                .Select(item => new SkillLookupSummary(item.Name, item.DisplayName)), request.Page, request.PageSize, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public async Task<SkillLookupSummary?> UpdateSkillLookupDisplayNameAsync(
        string gameVersion,
        string kind,
        string name,
        string displayName,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        switch (kind)
        {
            case "skill-operate-types":
            {
                var item = await context.SkillOperateTypes.SingleOrDefaultAsync(
                    value => value.GameVersion == gameVersion && value.Name == name, cancellationToken);
                if (item is null) return null;
                item.DisplayName = displayName;
                await context.SaveChangesAsync(cancellationToken);
                return new SkillLookupSummary(item.Name, item.DisplayName);
            }
            case "skill-target-types":
            {
                var item = await context.SkillTargetTypes.SingleOrDefaultAsync(
                    value => value.GameVersion == gameVersion && value.Name == name, cancellationToken);
                if (item is null) return null;
                item.DisplayName = displayName;
                await context.SaveChangesAsync(cancellationToken);
                return new SkillLookupSummary(item.Name, item.DisplayName);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public async Task<bool> DeleteSkillLookupAsync(
        string gameVersion,
        string kind,
        string name,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var usageCount = kind switch
        {
            "skill-operate-types" => await context.Skills.CountAsync(item => item.GameVersion == gameVersion && item.SkillOperateTypeName == name, cancellationToken),
            "skill-target-types" => await context.Skills.CountAsync(item => item.GameVersion == gameVersion && item.SkillTargetTypeName == name, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (usageCount > 0) throw new ContentDeleteConflictException("skill definitions", usageCount);
        var deleted = kind switch
        {
            "skill-operate-types" => await DeleteLookupAsync(context.SkillOperateTypes, gameVersion, name, cancellationToken),
            "skill-target-types" => await DeleteLookupAsync(context.SkillTargetTypes, gameVersion, name, cancellationToken),
            _ => false
        };
        if (!deleted) return false;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static async Task<bool> DeleteLookupAsync<TEntity>(
        DbSet<TEntity> set,
        string gameVersion,
        string name,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entity = await set.FindAsync([gameVersion, name], cancellationToken);
        if (entity is null) return false;
        set.Remove(entity);
        return true;
    }

    private static async Task<bool> DeletePlayerLookupAsync<TEntity>(
        DbSet<TEntity> set,
        string gameVersion,
        object id,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entity = await set.FindAsync([gameVersion, id], cancellationToken);
        if (entity is null) return false;
        set.Remove(entity);
        return true;
    }

    private static async Task<PlayerAppearanceSummary?> UpdatePlayerAppearanceAsync<TEntity>(
        GameContentDbContext context,
        DbSet<TEntity> set,
        string gameVersion,
        int id,
        int playerRaceId,
        int playerSexId,
        string name,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entity = await set.FindAsync([
            gameVersion,
            id,
            (PlayerSexId)playerSexId,
            (PlayerRaceId)playerRaceId
        ], cancellationToken);
        if (entity is null) return null;
        typeof(TEntity).GetProperty("Name")!.SetValue(entity, name);
        await context.SaveChangesAsync(cancellationToken);
        var raceName = await context.PlayerRaces.Where(value => value.GameVersion == gameVersion && (int)value.Id == playerRaceId)
            .Select(value => value.Name).SingleAsync(cancellationToken);
        var sexName = await context.PlayerSexes.Where(value => value.GameVersion == gameVersion && (int)value.Id == playerSexId)
            .Select(value => value.Name).SingleAsync(cancellationToken);
        return new PlayerAppearanceSummary(id, name, playerRaceId, raceName, playerSexId, sexName);
    }

    private static async Task<bool> DeletePlayerAppearanceAsync<TEntity>(
        DbSet<TEntity> set,
        string gameVersion,
        int id,
        int playerRaceId,
        int playerSexId,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entity = await set.FindAsync([
            gameVersion,
            id,
            (PlayerSexId)playerSexId,
            (PlayerRaceId)playerRaceId
        ], cancellationToken);
        if (entity is null) return false;
        set.Remove(entity);
        return true;
    }

    private static async Task<DirectoryPage<TItem>> PageAsync<TItem>(
        IQueryable<TItem> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await query.LongCountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue) return new DirectoryPage<TItem>([], total, page, pageSize);
        var items = await query.Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
        return new DirectoryPage<TItem>(items, total, page, pageSize);
    }

    private static IQueryable<NpcSummary> ProjectNpcs(IQueryable<L2.Studio.Context.Entities.Npc> npcs) =>
        npcs.Select(npc => new NpcSummary(
            npc.Id,
            npc.AppearanceId,
            npc.Level,
            npc.Name,
            npc.NpcTypeName,
            npc.NpcType.DisplayName,
            npc.NpcRaceName,
            npc.NpcRace == null ? null : npc.NpcRace.DisplayName,
            npc.NpcSexName,
            npc.NpcSex.DisplayName,
            false,
            npc.Status == null
                ? null
                : new NpcStatusSummary(
                    npc.Status.Attackable,
                    npc.Status.Targetable,
                    npc.Status.Talkable,
                    npc.Status.Undying,
                    npc.Status.ShowName,
                    npc.Status.RandomWalk,
                    npc.Status.CanMove,
                    npc.Status.NoSleepMode,
                    npc.Status.CanBeSown),
            npc.Stats == null ? null : new NpcStatsSummary(npc.Stats.Str, npc.Stats.Int, npc.Stats.Dex, npc.Stats.Wit, npc.Stats.Con, npc.Stats.Men, npc.Stats.HitTime),
            npc.StatsVitals == null ? null : new NpcStatsVitalsSummary(npc.StatsVitals.Hp, npc.StatsVitals.HpRegen, npc.StatsVitals.Mp, npc.StatsVitals.MpRegen),
            npc.StatsAttack == null ? null : new NpcStatsAttackSummary(npc.StatsAttack.Physical, npc.StatsAttack.Magical, npc.StatsAttack.Random, npc.StatsAttack.Critical, npc.StatsAttack.Accuracy, npc.StatsAttack.AttackSpeed, npc.StatsAttack.ReuseDelay, npc.StatsAttack.Type, npc.StatsAttack.Range, npc.StatsAttack.Distance, npc.StatsAttack.Width),
            npc.StatsDefence == null ? null : new NpcStatsDefenceSummary(npc.StatsDefence.Physical, npc.StatsDefence.Magical, npc.StatsDefence.Evasion, npc.StatsDefence.Shield, npc.StatsDefence.ShieldRate),
            npc.StatsSpeed == null ? null : new NpcStatsSpeedSummary(npc.StatsSpeed.WalkGround, npc.StatsSpeed.RunGround)));

    private static IReadOnlyList<NpcSummary> WithVisuals(
        IReadOnlyList<NpcSummary> npcs,
        IReadOnlySet<int> visualNpcIds) =>
        npcs.Select(npc => WithVisuals(npc, visualNpcIds)).ToArray();

    private static NpcSummary WithVisuals(NpcSummary npc, IReadOnlySet<int> visualNpcIds) =>
        npc with { HasVisuals = visualNpcIds.Contains(npc.Id) };

    private static async Task<IReadOnlySet<int>> NpcVisualIdsAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        var catalog = await context.AssetCatalogs.AsNoTracking()
            .Where(catalog => catalog.GameVersion == gameVersion &&
                catalog.Kind == AssetImportJobValues.NpcAppearances && catalog.IsActive)
            .Select(catalog => new { catalog.SchemaVersion, catalog.MetadataJson })
            .SingleOrDefaultAsync(cancellationToken);
        if (catalog is null || catalog.SchemaVersion < 6) return new HashSet<int>();

        using var document = JsonDocument.Parse(catalog.MetadataJson);
        if (!document.RootElement.TryGetProperty("npcIds", out var npcIds) ||
            npcIds.ValueKind != JsonValueKind.Array)
            return new HashSet<int>();
        return npcIds.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _))
            .Select(value => value.GetInt32())
            .ToHashSet();
    }
}

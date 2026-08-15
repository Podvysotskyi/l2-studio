using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
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

    public async Task<IReadOnlyList<NpcLookupSummary>> GetNpcTypesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.NpcTypes
            .AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .OrderBy(item => item.Name)
            .Select(item => new NpcLookupSummary(item.Name, item.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NpcLookupSummary>> GetNpcRacesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.NpcRaces
            .AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .OrderBy(item => item.Name)
            .Select(item => new NpcLookupSummary(item.Name, item.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NpcLookupSummary>> GetNpcSexesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.NpcSexes
            .AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .OrderBy(item => item.Name)
            .Select(item => new NpcLookupSummary(item.Name, item.DisplayName))
            .ToListAsync(cancellationToken);
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

    public async Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerRacesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.PlayerRaces.AsNoTracking()
            .Where(item => item.GameVersion == gameVersion).OrderBy(item => item.Id)
            .Select(item => new PlayerLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerSexesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.PlayerSexes.AsNoTracking()
            .Where(item => item.GameVersion == gameVersion).OrderBy(item => item.Id)
            .Select(item => new PlayerLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerAppearanceSummary>> GetPlayerFacesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.PlayerFaces.AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .OrderBy(item => item.PlayerRaceId).ThenBy(item => item.PlayerSexId).ThenBy(item => item.Id)
            .Select(item => new PlayerAppearanceSummary(
                item.Id, item.Name, (int)item.PlayerRaceId, item.PlayerRace.Name, (int)item.PlayerSexId, item.PlayerSex.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerAppearanceSummary>> GetPlayerHairStylesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.PlayerHairStyles.AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .OrderBy(item => item.PlayerRaceId).ThenBy(item => item.PlayerSexId).ThenBy(item => item.Id)
            .Select(item => new PlayerAppearanceSummary(
                item.Id, item.Name, (int)item.PlayerRaceId, item.PlayerRace.Name, (int)item.PlayerSexId, item.PlayerSex.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerAppearanceSummary>> GetPlayerHairColorsAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.PlayerHairColors.AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .OrderBy(item => item.PlayerRaceId).ThenBy(item => item.PlayerSexId).ThenBy(item => item.Id)
            .Select(item => new PlayerAppearanceSummary(
                item.Id, item.Name, (int)item.PlayerRaceId, item.PlayerRace.Name, (int)item.PlayerSexId, item.PlayerSex.Name))
            .ToListAsync(cancellationToken);
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
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var skills = context.Skills
            .AsNoTracking()
            .Where(skill => skill.GameVersion == gameVersion &&
                (query == string.Empty || EF.Functions.ILike(skill.Name, searchPattern, "\\")));
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
                (int?)skill.SkillOperateTypeId,
                skill.SkillOperateType == null ? null : skill.SkillOperateType.Name,
                (int?)skill.SkillTargetTypeId,
                skill.SkillTargetType == null ? null : skill.SkillTargetType.Name,
                skill.SkillIcons.Count))
            .ToListAsync(cancellationToken);

        return new SkillDirectoryPage(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<SkillLookupSummary>> GetSkillOperateTypesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SkillOperateTypes
            .AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .OrderBy(item => item.Id)
            .Select(item => new SkillLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillLookupSummary>> GetSkillTargetTypesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SkillTargetTypes
            .AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .OrderBy(item => item.Id)
            .Select(item => new SkillLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

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

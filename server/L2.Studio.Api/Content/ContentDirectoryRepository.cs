using L2.Studio.Content;
using L2.Studio.Contracts;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Api.Content;

public sealed class ContentDirectoryRepository(
    IDbContextFactory<GameContentDbContext> contextFactory)
{
    public async Task<NpcDirectoryPage> SearchNpcsAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var offset = ((long)page - 1) * pageSize;
        var searchPattern = $"%{EscapeLikePattern(query)}%";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var npcs = context.Npcs
            .AsNoTracking()
            .Where(npc => query == string.Empty ||
                (npc.Name != null && EF.Functions.ILike(npc.Name, searchPattern, "\\")));
        var total = await npcs.LongCountAsync(cancellationToken);
        if (offset > int.MaxValue)
        {
            return new NpcDirectoryPage([], total, page, pageSize);
        }

        var items = await npcs
            .OrderBy(npc => npc.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .Select(npc => new NpcSummary(
                npc.Id,
                npc.Level,
                npc.Name,
                (int)npc.NpcTypeId,
                npc.NpcType.Name,
                (int?)npc.NpcRaceId,
                npc.NpcRace == null ? null : npc.NpcRace.Name,
                (int)npc.NpcSexId,
                npc.NpcSex.Name))
            .ToListAsync(cancellationToken);

        return new NpcDirectoryPage(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<NpcLookupSummary>> GetNpcTypesAsync(
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.NpcTypes
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new NpcLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NpcLookupSummary>> GetNpcRacesAsync(
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.NpcRaces
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new NpcLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NpcLookupSummary>> GetNpcSexesAsync(
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.NpcSexes
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new NpcLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerClassSummary>> GetPlayerClassesAsync(
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var variants = await context.PlayerClasses
            .AsNoTracking()
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
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.PlayerRaces.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new PlayerLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerSexesAsync(
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.PlayerSexes.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new PlayerLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<SkillDirectoryPage> SearchSkillsAsync(
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
            .Where(skill => query == string.Empty || EF.Functions.ILike(skill.Name, searchPattern, "\\"));
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
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SkillOperateTypes
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new SkillLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillLookupSummary>> GetSkillTargetTypesAsync(
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SkillTargetTypes
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new SkillLookupSummary((int)item.Id, item.Name))
            .ToListAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}

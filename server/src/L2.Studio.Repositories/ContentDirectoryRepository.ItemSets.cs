using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Repositories;

public sealed partial class ContentDirectoryRepository
{
    public async Task<ItemSetDirectoryPage> SearchItemSetsAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim() ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        var offset = ((long)request.Page - 1) * request.PageSize;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var sets = context.ItemSets.AsNoTracking().Where(value => value.GameVersion == gameVersion);
        if (query != string.Empty)
        {
            var isSetId = int.TryParse(query, out var setId);
            sets = sets.Where(value => (isSetId && value.SetId == setId) || value.BodyParts.Any(part =>
                context.Items.Any(item => item.GameVersion == gameVersion && item.Id == part.ItemId &&
                    EF.Functions.ILike(item.Name, pattern, "\\"))));
        }
        var total = await sets.LongCountAsync(cancellationToken);
        if (offset > int.MaxValue) return new ItemSetDirectoryPage([], total, request.Page, request.PageSize);
        var items = await ProjectItemSets(sets.OrderBy(value => value.SetId).Skip((int)offset).Take(request.PageSize), context.Items, context.Skills)
            .ToListAsync(cancellationToken);
        return new ItemSetDirectoryPage(items, total, request.Page, request.PageSize);
    }

    public async Task<ItemSetSummary?> GetItemSetAsync(string gameVersion, int setId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await ProjectItemSets(context.ItemSets.AsNoTracking().Where(value => value.GameVersion == gameVersion && value.SetId == setId), context.Items, context.Skills)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ItemSetSummary?> UpdateItemSetAsync(string gameVersion, int setId, UpdateItemSetRequest request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var itemSet = await context.ItemSets.Include(value => value.Skills).Include(value => value.Stats)
            .SingleOrDefaultAsync(value => value.GameVersion == gameVersion && value.SetId == setId, cancellationToken);
        if (itemSet is null) return null;
        var skill = await context.Skills.AsNoTracking().SingleOrDefaultAsync(
            value => value.GameVersion == gameVersion && value.Id == request.SkillId, cancellationToken);
        if (skill is null || request.SkillLevel < 1 || request.SkillLevel > skill.Levels)
            throw new InvalidOperationException("Select a skill and a level supported by that skill.");

        context.ItemSetSkills.RemoveRange(itemSet.Skills);
        itemSet.Skills.Clear();
        itemSet.Skills.Add(new ItemSetSkill
        {
            GameVersion = gameVersion, SetId = setId, SkillId = request.SkillId, SkillLevel = request.SkillLevel
        });
        if (request.Str is null && request.Dex is null && request.Con is null && request.Int is null && request.Wit is null && request.Men is null)
        {
            if (itemSet.Stats is not null) context.ItemSetStats.Remove(itemSet.Stats);
            itemSet.Stats = null;
        }
        else
        {
            itemSet.Stats ??= new ItemSetStats { GameVersion = gameVersion, SetId = setId };
            itemSet.Stats.Str = request.Str; itemSet.Stats.Dex = request.Dex; itemSet.Stats.Con = request.Con;
            itemSet.Stats.Int = request.Int; itemSet.Stats.Wit = request.Wit; itemSet.Stats.Men = request.Men;
        }
        await context.SaveChangesAsync(cancellationToken);
        return await GetItemSetAsync(gameVersion, setId, cancellationToken);
    }

    private static IQueryable<ItemSetSummary> ProjectItemSets(IQueryable<ItemSet> sets, IQueryable<Item> items, IQueryable<Skill> skills) => sets.Select(value => new ItemSetSummary(
        value.SetId,
        value.BodyParts.OrderBy(part => part.BodyPartName).Select(part => new ItemSetBodyPartSummary(
            part.BodyPartName, part.BodyPart.DisplayName, part.ItemId,
            items.Where(item => item.GameVersion == value.GameVersion && item.Id == part.ItemId).Select(item => item.Name).FirstOrDefault())).ToArray(),
        value.Skills.OrderBy(skill => skill.SkillId).Select(skill => new ItemSetSkillSummary(
            skill.SkillId, skill.SkillLevel,
            skills.Where(item => item.GameVersion == value.GameVersion && item.Id == skill.SkillId).Select(item => item.Name).FirstOrDefault(),
            skills.Where(item => item.GameVersion == value.GameVersion && item.Id == skill.SkillId).Select(item => (short?)item.Levels).FirstOrDefault())).FirstOrDefault(),
        value.Stats == null ? null : new ItemSetStatsSummary(
            value.Stats.Str, value.Stats.Dex, value.Stats.Con, value.Stats.Int, value.Stats.Wit, value.Stats.Men)));
}

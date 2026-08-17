using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class ItemSetImportHandlers(IDbContextFactory<GameContentDbContext> contextFactory, TimeProvider timeProvider)
{
    private static readonly C1ItemSetCatalog Catalog = new();

    public Task Handle(ImportC1ItemSets message, CancellationToken token) => ImportAsync(message.RunId, token);

    private async Task ImportAsync(Guid runId, CancellationToken token)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(token);
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var run = await context.ContentImportRuns.SingleOrDefaultAsync(value =>
                value.Id == runId && value.Kind == ContentImportTargetValues.ItemSets, token);
            if (run is null || ItemImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            if (run.GameVersion != "c1" || !ItemImportJobValues.SupportedModes.Contains(run.Mode)) throw new InvalidOperationException("Only C1 add-missing and restore-defaults item-set imports are supported.");
            run.Status = ItemImportJobValues.Running;
            run.StartedAt ??= timeProvider.GetUtcNow();
            run.LastHeartbeatAt = timeProvider.GetUtcNow();
            await EnsureDependenciesAsync(context, run.GameVersion, token);
            var existing = await context.ItemSets
                .Include(value => value.BodyParts).Include(value => value.Skills).Include(value => value.Stats)
                .Where(value => value.GameVersion == run.GameVersion).ToDictionaryAsync(value => value.SetId, token);
            var missing = Catalog.ItemSets.Where(definition => !existing.ContainsKey(definition.SetId)).ToArray();
            context.ItemSets.AddRange(missing.Select(definition => ToEntity(run.GameVersion, definition)));
            var restored = Array.Empty<ItemSetDefinition>();
            if (run.Mode == ItemImportJobValues.RestoreDefaults)
            {
                restored = Catalog.ItemSets.Where(definition => existing.ContainsKey(definition.SetId)).ToArray();
                foreach (var definition in restored) Restore(context, existing[definition.SetId], definition);
            }
            run.TotalCount = Catalog.ItemSets.Count;
            run.InsertedCount = missing.Length;
            run.ExistingCount = Catalog.ItemSets.Count - missing.Length;
            run.RestoredCount = restored.Length;
            run.Status = ItemImportJobValues.Succeeded;
            run.FinishedAt = timeProvider.GetUtcNow();
            run.LastHeartbeatAt = run.FinishedAt;
            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await MarkFailed(runId, exception, token);
        }
    }

    private static async Task EnsureDependenciesAsync(GameContentDbContext context, string gameVersion, CancellationToken token)
    {
        var requiredBodyParts = Catalog.ItemSets.SelectMany(value => value.BodyParts).Select(value => value.BodyPartName).ToHashSet(StringComparer.Ordinal);
        var bodyParts = await context.ItemBodyParts.Where(value => value.GameVersion == gameVersion).Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        var missingBodyParts = requiredBodyParts.Where(value => !bodyParts.Contains(value)).Order().ToArray();
        if (missingBodyParts.Length > 0) throw new InvalidOperationException($"Missing item body parts: {string.Join(", ", missingBodyParts)}.");
        var skills = await context.Skills.Where(value => value.GameVersion == gameVersion).ToDictionaryAsync(value => value.Id, value => value.Levels, token);
        var invalidSkills = Catalog.ItemSets.Select(value => value.Skill).Where(value => !skills.TryGetValue(value.SkillId, out var levels) || value.SkillLevel < 1 || value.SkillLevel > levels).ToArray();
        if (invalidSkills.Length > 0) throw new InvalidOperationException("Missing or invalid item-set skill definitions.");
    }

    private static ItemSet ToEntity(string gameVersion, ItemSetDefinition definition)
    {
        var itemSet = new ItemSet { GameVersion = gameVersion, SetId = definition.SetId };
        Apply(itemSet, definition);
        return itemSet;
    }

    private static void Restore(GameContentDbContext context, ItemSet itemSet, ItemSetDefinition definition)
    {
        context.ItemSetBodyParts.RemoveRange(itemSet.BodyParts);
        context.ItemSetSkills.RemoveRange(itemSet.Skills);
        if (itemSet.Stats is not null) context.ItemSetStats.Remove(itemSet.Stats);
        itemSet.BodyParts.Clear(); itemSet.Skills.Clear(); itemSet.Stats = null;
        Apply(itemSet, definition);
    }

    private static void Apply(ItemSet itemSet, ItemSetDefinition definition)
    {
        foreach (var part in definition.BodyParts)
            itemSet.BodyParts.Add(new ItemSetBodyPart { GameVersion = itemSet.GameVersion, SetId = itemSet.SetId, BodyPartName = part.BodyPartName, ItemId = part.ItemId });
        itemSet.Skills.Add(new ItemSetSkill { GameVersion = itemSet.GameVersion, SetId = itemSet.SetId, SkillId = definition.Skill.SkillId, SkillLevel = definition.Skill.SkillLevel });
        if (definition.Stats is { } stats)
            itemSet.Stats = new ItemSetStats { GameVersion = itemSet.GameVersion, SetId = itemSet.SetId, Str = stats.Str, Dex = stats.Dex, Con = stats.Con, Int = stats.Int, Wit = stats.Wit, Men = stats.Men };
    }

    private async Task MarkFailed(Guid runId, Exception exception, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var run = await context.ContentImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
        if (run is null || ItemImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = ItemImportJobValues.Failed;
        run.Error = exception.ToString()[..Math.Min(exception.ToString().Length, 4000)];
        run.FinishedAt = timeProvider.GetUtcNow();
        run.LastHeartbeatAt = run.FinishedAt;
        await context.SaveChangesAsync(token);
    }
}

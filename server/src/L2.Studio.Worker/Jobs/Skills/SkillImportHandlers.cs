using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class SkillImportHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    TimeProvider timeProvider)
{
    private static readonly C1SkillCatalog Catalog = new();

    public Task Handle(ImportC1Skills message, CancellationToken token) => ImportAsync(message.RunId, token);

    private async Task ImportAsync(Guid runId, CancellationToken token)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(token);
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var run = await context.SkillImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
            if (run is null || SkillImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            if (run.GameVersion != "c1" || !SkillImportJobValues.SupportedModes.Contains(run.Mode))
                throw new InvalidOperationException("Only C1 add-missing and restore-defaults skill imports are supported.");

            run.Status = SkillImportJobValues.Running;
            run.StartedAt ??= timeProvider.GetUtcNow();
            await EnsureLookupsAsync(context, run.GameVersion, run.Mode == SkillImportJobValues.RestoreDefaults, token);
            var existing = await context.Skills.Include(skill => skill.SkillIcons)
                .Where(skill => skill.GameVersion == run.GameVersion).ToDictionaryAsync(skill => skill.Id, token);
            var missing = Catalog.Skills.Where(definition => !existing.ContainsKey(definition.Id)).ToArray();
            context.Skills.AddRange(missing.Select(definition => ToEntity(run.GameVersion, definition)));

            var restored = Array.Empty<SkillDefinition>();
            if (run.Mode == SkillImportJobValues.RestoreDefaults)
            {
                restored = Catalog.Skills.Where(definition => existing.ContainsKey(definition.Id)).ToArray();
                foreach (var definition in restored) Apply(context, existing[definition.Id], definition, restoreDefaults: true);
            }
            else
            {
                foreach (var definition in Catalog.Skills.Where(definition => existing.ContainsKey(definition.Id)))
                    AddMissingIcons(context, existing[definition.Id], definition);
            }

            run.TotalCount = Catalog.Skills.Count;
            run.InsertedCount = missing.Length;
            run.ExistingCount = Catalog.Skills.Count - missing.Length;
            run.RestoredCount = restored.Length;
            run.Status = SkillImportJobValues.Succeeded;
            run.FinishedAt = timeProvider.GetUtcNow();
            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await MarkFailedAsync(runId, exception, token);
        }
    }

    private static async Task EnsureLookupsAsync(
        GameContentDbContext context, string gameVersion, bool restoreDefaults, CancellationToken token)
    {
        var operateTypes = await context.SkillOperateTypes.Where(value => value.GameVersion == gameVersion)
            .ToDictionaryAsync(value => value.Id, token);
        foreach (var definition in Catalog.OperateTypes)
        {
            if (operateTypes.TryGetValue(definition.Id, out var existing))
            {
                if (restoreDefaults) existing.Name = definition.Name;
            }
            else
            {
                context.SkillOperateTypes.Add(new SkillOperateType
                {
                    GameVersion = gameVersion, Id = definition.Id, Name = definition.Name
                });
            }
        }

        var targetTypes = await context.SkillTargetTypes.Where(value => value.GameVersion == gameVersion)
            .ToDictionaryAsync(value => value.Id, token);
        foreach (var definition in Catalog.TargetTypes)
        {
            if (targetTypes.TryGetValue(definition.Id, out var existing))
            {
                if (restoreDefaults) existing.Name = definition.Name;
            }
            else
            {
                context.SkillTargetTypes.Add(new SkillTargetType
                {
                    GameVersion = gameVersion, Id = definition.Id, Name = definition.Name
                });
            }
        }
    }

    private static Skill ToEntity(string gameVersion, SkillDefinition definition) => new()
    {
        GameVersion = gameVersion,
        Id = definition.Id,
        Levels = definition.Levels,
        Name = definition.Name,
        SkillOperateTypeId = definition.OperateTypeId,
        SkillTargetTypeId = definition.TargetTypeId,
        SkillIcons = definition.Icons.Select(icon => ToEntity(gameVersion, definition.Id, icon)).ToArray()
    };

    private static SkillIcon ToEntity(string gameVersion, int skillId, SkillIconDefinition definition) => new()
    {
        GameVersion = gameVersion, SkillId = skillId, Level = definition.Level, Name = definition.Name
    };

    private static void Apply(
        GameContentDbContext context, Skill skill, SkillDefinition definition, bool restoreDefaults)
    {
        skill.Levels = definition.Levels;
        skill.Name = definition.Name;
        skill.SkillOperateTypeId = definition.OperateTypeId;
        skill.SkillTargetTypeId = definition.TargetTypeId;
        if (restoreDefaults)
        {
            var sourceIcons = definition.Icons.ToDictionary(icon => icon.Level);
            foreach (var icon in skill.SkillIcons.ToArray())
            {
                if (sourceIcons.Remove(icon.Level, out var source)) icon.Name = source.Name;
                else context.SkillIcons.Remove(icon);
            }
            foreach (var source in sourceIcons.Values)
            {
                var icon = ToEntity(skill.GameVersion, skill.Id, source);
                skill.SkillIcons.Add(icon);
                context.SkillIcons.Add(icon);
            }
        }
        else
        {
            AddMissingIcons(context, skill, definition);
        }
    }

    private static void AddMissingIcons(GameContentDbContext context, Skill skill, SkillDefinition definition)
    {
        var existingLevels = skill.SkillIcons.Select(icon => icon.Level).ToHashSet();
        foreach (var definitionIcon in definition.Icons.Where(icon => !existingLevels.Contains(icon.Level)))
        {
            var icon = ToEntity(skill.GameVersion, skill.Id, definitionIcon);
            skill.SkillIcons.Add(icon);
            context.SkillIcons.Add(icon);
        }
    }

    private async Task MarkFailedAsync(Guid runId, Exception exception, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var run = await context.SkillImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
        if (run is null || SkillImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = SkillImportJobValues.Failed;
        run.Error = exception.ToString()[..Math.Min(exception.ToString().Length, 4000)];
        run.FinishedAt = timeProvider.GetUtcNow();
        await context.SaveChangesAsync(token);
    }
}

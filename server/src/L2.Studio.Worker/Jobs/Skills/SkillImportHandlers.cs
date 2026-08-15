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

    public Task Handle(ImportC1Skills message, CancellationToken token) =>
        ImportAsync(message.RunId, ContentImportTargetValues.Skills, token);
    public Task Handle(ImportC1SkillOperateTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, ContentImportTargetValues.SkillOperateTypes, token);
    public Task Handle(ImportC1SkillTargetTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, ContentImportTargetValues.SkillTargetTypes, token);

    private async Task ImportAsync(Guid runId, string target, CancellationToken token)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(token);
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var run = await context.ContentImportRuns.SingleOrDefaultAsync(
                value => value.Id == runId && value.Kind == target, token);
            if (run is null || ImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            if (run.GameVersion != "c1" || !ImportJobValues.ContentModes.Contains(run.Mode))
                throw new InvalidOperationException("Only C1 add-missing and restore-defaults skill imports are supported.");

            run.Status = ImportJobValues.Running;
            run.StartedAt ??= timeProvider.GetUtcNow();
            run.LastHeartbeatAt = timeProvider.GetUtcNow();
            var restoreDefaults = run.Mode == ImportJobValues.RestoreDefaults;
            var counts = target switch
            {
                ContentImportTargetValues.SkillOperateTypes =>
                    await ImportOperateTypesAsync(context, run.GameVersion, restoreDefaults, token),
                ContentImportTargetValues.SkillTargetTypes =>
                    await ImportTargetTypesAsync(context, run.GameVersion, restoreDefaults, token),
                ContentImportTargetValues.Skills =>
                    await ImportSkillsAsync(context, run.GameVersion, restoreDefaults, token),
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };
            run.TotalCount = counts.Total;
            run.InsertedCount = counts.Inserted;
            run.ExistingCount = counts.Existing;
            run.RestoredCount = counts.Restored;
            run.Status = ImportJobValues.Succeeded;
            run.FinishedAt = timeProvider.GetUtcNow();
            run.LastHeartbeatAt = run.FinishedAt;
            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await MarkFailedAsync(runId, exception, token);
        }
    }

    private static async Task<ImportCounts> ImportSkillsAsync(
        GameContentDbContext context,
        string gameVersion,
        bool restoreDefaults,
        CancellationToken token)
    {
        await ImportOperateTypesAsync(context, gameVersion, false, token);
        await ImportTargetTypesAsync(context, gameVersion, false, token);
        await context.SaveChangesAsync(token);

        var existing = await context.Skills.Include(skill => skill.SkillIcons)
            .Where(skill => skill.GameVersion == gameVersion).ToDictionaryAsync(skill => skill.Id, token);
        var missing = Catalog.Skills.Where(definition => !existing.ContainsKey(definition.Id)).ToArray();
        context.Skills.AddRange(missing.Select(definition => ToEntity(gameVersion, definition)));

        var restored = 0;
        foreach (var definition in Catalog.Skills.Where(definition => existing.ContainsKey(definition.Id)))
        {
            if (restoreDefaults)
            {
                Apply(context, existing[definition.Id], definition);
                restored++;
            }
            else
            {
                AddMissingIcons(context, existing[definition.Id], definition);
            }
        }
        return new ImportCounts(Catalog.Skills.Count, missing.Length, Catalog.Skills.Count - missing.Length, restored);
    }

    private static async Task<ImportCounts> ImportOperateTypesAsync(
        GameContentDbContext context,
        string gameVersion,
        bool restoreDefaults,
        CancellationToken token)
    {
        var existing = await context.SkillOperateTypes.Where(value => value.GameVersion == gameVersion)
            .ToDictionaryAsync(value => value.Name, StringComparer.Ordinal, token);
        var reconciliation = Reconcile(Catalog.OperateTypes,
            existing.ToDictionary(value => value.Key, value => value.Value.DisplayName, StringComparer.Ordinal),
            value => value.Name, value => value.DisplayName, restoreDefaults);
        foreach (var restored in reconciliation.Restored)
            existing[restored.Key].DisplayName = restored.Value;
        context.SkillOperateTypes.AddRange(reconciliation.Missing.Select(definition => new SkillOperateType
        {
            GameVersion = gameVersion, Name = definition.Name, DisplayName = definition.DisplayName
        }));
        return new ImportCounts(
            Catalog.OperateTypes.Count,
            reconciliation.Missing.Length,
            Catalog.OperateTypes.Count - reconciliation.Missing.Length,
            reconciliation.Restored.Count);
    }

    private static async Task<ImportCounts> ImportTargetTypesAsync(
        GameContentDbContext context,
        string gameVersion,
        bool restoreDefaults,
        CancellationToken token)
    {
        var existing = await context.SkillTargetTypes.Where(value => value.GameVersion == gameVersion)
            .ToDictionaryAsync(value => value.Name, StringComparer.Ordinal, token);
        var reconciliation = Reconcile(Catalog.TargetTypes,
            existing.ToDictionary(value => value.Key, value => value.Value.DisplayName, StringComparer.Ordinal),
            value => value.Name, value => value.DisplayName, restoreDefaults);
        foreach (var restored in reconciliation.Restored)
            existing[restored.Key].DisplayName = restored.Value;
        context.SkillTargetTypes.AddRange(reconciliation.Missing.Select(definition => new SkillTargetType
        {
            GameVersion = gameVersion, Name = definition.Name, DisplayName = definition.DisplayName
        }));
        return new ImportCounts(
            Catalog.TargetTypes.Count,
            reconciliation.Missing.Length,
            Catalog.TargetTypes.Count - reconciliation.Missing.Length,
            reconciliation.Restored.Count);
    }

    internal static (TDefinition[] Missing, Dictionary<string, string> Restored) Reconcile<TDefinition>(
        IReadOnlyList<TDefinition> definitions,
        IReadOnlyDictionary<string, string> existing,
        Func<TDefinition, string> name,
        Func<TDefinition, string> displayName,
        bool restoreDefaults)
    {
        var missing = definitions.Where(definition => !existing.ContainsKey(name(definition))).ToArray();
        var restored = restoreDefaults
            ? definitions.Where(definition => existing.TryGetValue(name(definition), out var currentDisplayName) &&
                    currentDisplayName != displayName(definition))
                .ToDictionary(name, displayName, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        return (missing, restored);
    }

    private static Skill ToEntity(string gameVersion, SkillDefinition definition) => new()
    {
        GameVersion = gameVersion,
        Id = definition.Id,
        Levels = definition.Levels,
        Name = definition.Name,
        SkillOperateTypeName = definition.OperateTypeName,
        SkillTargetTypeName = definition.TargetTypeName,
        SkillIcons = definition.Icons.Select(icon => ToEntity(gameVersion, definition.Id, icon)).ToArray()
    };

    private static SkillIcon ToEntity(string gameVersion, int skillId, SkillIconDefinition definition) => new()
    {
        GameVersion = gameVersion, SkillId = skillId, Level = definition.Level, Name = definition.Name
    };

    private static void Apply(GameContentDbContext context, Skill skill, SkillDefinition definition)
    {
        skill.Levels = definition.Levels;
        skill.Name = definition.Name;
        skill.SkillOperateTypeName = definition.OperateTypeName;
        skill.SkillTargetTypeName = definition.TargetTypeName;
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
        var run = await context.ContentImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
        if (run is null || ImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = ImportJobValues.Failed;
        run.Error = exception.ToString()[..Math.Min(exception.ToString().Length, 4000)];
        run.FinishedAt = timeProvider.GetUtcNow();
        run.LastHeartbeatAt = run.FinishedAt;
        await context.SaveChangesAsync(token);
    }

    private sealed record ImportCounts(int Total, int Inserted, int Existing, int Restored);
}

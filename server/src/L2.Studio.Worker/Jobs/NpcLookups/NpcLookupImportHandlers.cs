using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class NpcLookupImportHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    TimeProvider timeProvider)
{
    private static readonly NpcLookupCatalog C1Catalog = new C1NpcLookupCatalog();
    private static readonly NpcLookupCatalog C4Catalog = new C4NpcLookupCatalog();
    private static readonly NpcLookupCatalog InterludeCatalog = new InterludeNpcLookupCatalog();

    public Task Handle(ImportC1NpcTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcTypes, C1Catalog.Types, token);

    public Task Handle(ImportC4NpcTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcTypes, C4Catalog.Types, token);

    public Task Handle(ImportInterludeNpcTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcTypes, InterludeCatalog.Types, token);

    public Task Handle(ImportC1NpcRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcRaces, C1Catalog.Races, token);

    public Task Handle(ImportC4NpcRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcRaces, C4Catalog.Races, token);

    public Task Handle(ImportInterludeNpcRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcRaces, InterludeCatalog.Races, token);

    public Task Handle(ImportC1NpcSexes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcSexes, C1Catalog.Sexes, token);

    public Task Handle(ImportC4NpcSexes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcSexes, C4Catalog.Sexes, token);

    public Task Handle(ImportInterludeNpcSexes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcSexes, InterludeCatalog.Sexes, token);

    private async Task ImportAsync(
        Guid runId,
        string kind,
        IReadOnlyList<NpcLookupDefinition> definitions,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var run = await context.ContentImportRuns.SingleOrDefaultAsync(
                value => value.Id == runId && value.Kind == kind, cancellationToken);
            if (run is null || NpcLookupImportJobValues.TerminalStatuses.Contains(run.Status)) return;

            var now = timeProvider.GetUtcNow();
            run.Status = NpcLookupImportJobValues.Running;
            run.StartedAt ??= now;
            run.LastHeartbeatAt = now;
            NpcLookupDefinition[] missing;
            int existingCount;
            int restoredCount;
            if (kind == NpcLookupImportJobValues.NpcTypes)
            {
                var existing = await context.NpcTypes.Where(item => item.GameVersion == run.GameVersion)
                    .ToDictionaryAsync(item => item.Name, StringComparer.Ordinal, cancellationToken);
                var reconciliation = Reconcile(definitions,
                    existing.ToDictionary(item => item.Key, item => item.Value.DisplayName, StringComparer.Ordinal),
                    run.Mode == NpcLookupImportJobValues.RestoreDefaults);
                missing = reconciliation.Missing;
                existingCount = definitions.Count - missing.Length;
                restoredCount = reconciliation.Restored.Count;
                foreach (var restored in reconciliation.Restored)
                    existing[restored.Key].DisplayName = restored.Value;
                context.NpcTypes.AddRange(missing.Select(definition => new NpcType
                {
                    GameVersion = run.GameVersion,
                    Name = definition.Name,
                    DisplayName = definition.DisplayName
                }));
            }
            else if (kind == NpcLookupImportJobValues.NpcRaces)
            {
                var existing = await context.NpcRaces.Where(item => item.GameVersion == run.GameVersion)
                    .ToDictionaryAsync(item => item.Name, StringComparer.Ordinal, cancellationToken);
                var reconciliation = Reconcile(definitions,
                    existing.ToDictionary(item => item.Key, item => item.Value.DisplayName, StringComparer.Ordinal),
                    run.Mode == NpcLookupImportJobValues.RestoreDefaults);
                missing = reconciliation.Missing;
                existingCount = definitions.Count - missing.Length;
                restoredCount = reconciliation.Restored.Count;
                foreach (var restored in reconciliation.Restored)
                    existing[restored.Key].DisplayName = restored.Value;
                context.NpcRaces.AddRange(missing.Select(definition => new NpcRace
                {
                    GameVersion = run.GameVersion,
                    Name = definition.Name,
                    DisplayName = definition.DisplayName
                }));
            }
            else
            {
                var existing = await context.NpcSexes.Where(item => item.GameVersion == run.GameVersion)
                    .ToDictionaryAsync(item => item.Name, StringComparer.Ordinal, cancellationToken);
                var reconciliation = Reconcile(definitions,
                    existing.ToDictionary(item => item.Key, item => item.Value.DisplayName, StringComparer.Ordinal),
                    run.Mode == NpcLookupImportJobValues.RestoreDefaults);
                missing = reconciliation.Missing;
                existingCount = definitions.Count - missing.Length;
                restoredCount = reconciliation.Restored.Count;
                foreach (var restored in reconciliation.Restored)
                    existing[restored.Key].DisplayName = restored.Value;
                context.NpcSexes.AddRange(missing.Select(definition => new NpcSex
                {
                    GameVersion = run.GameVersion,
                    Name = definition.Name,
                    DisplayName = definition.DisplayName
                }));
            }

            run.TotalCount = definitions.Count;
            run.InsertedCount = missing.Length;
            run.ExistingCount = existingCount;
            run.RestoredCount = restoredCount;
            run.Status = NpcLookupImportJobValues.Succeeded;
            run.FinishedAt = timeProvider.GetUtcNow();
            run.LastHeartbeatAt = run.FinishedAt;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await MarkFailedAsync(runId, exception, cancellationToken);
        }
    }

    internal static (NpcLookupDefinition[] Missing, Dictionary<string, string> Restored) Reconcile(
        IReadOnlyList<NpcLookupDefinition> definitions,
        IReadOnlyDictionary<string, string> existing,
        bool restoreDefaults)
    {
        var missing = definitions.Where(definition => !existing.ContainsKey(definition.Name)).ToArray();
        var restored = restoreDefaults
            ? definitions.Where(definition => existing.TryGetValue(definition.Name, out var displayName) &&
                    displayName != definition.DisplayName)
                .ToDictionary(definition => definition.Name, definition => definition.DisplayName, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        return (missing, restored);
    }

    private async Task MarkFailedAsync(Guid runId, Exception exception, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = await context.ContentImportRuns.SingleOrDefaultAsync(value => value.Id == runId, cancellationToken);
        if (run is null || NpcLookupImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = NpcLookupImportJobValues.Failed;
        run.FinishedAt = timeProvider.GetUtcNow();
        run.LastHeartbeatAt = run.FinishedAt;
        run.Error = exception.Message.Length <= 4000 ? exception.Message : exception.Message[..4000];
        await context.SaveChangesAsync(cancellationToken);
    }
}

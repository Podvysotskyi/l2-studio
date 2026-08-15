using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class ItemLookupImportHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    TimeProvider timeProvider)
{
    private static readonly C1ItemCatalog Catalog = new();

    public Task Handle(ImportC1ItemTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, ItemLookupImportJobValues.ItemTypes, Catalog.Types,
            context => context.ItemTypes, context => context.ItemTypes,
            item => item.Name, item => item.DisplayName,
            (item, definition) => item.DisplayName = definition.DisplayName,
            (version, definition) => new ItemType
            {
                GameVersion = version, Name = definition.Name, DisplayName = definition.DisplayName
            }, token);

    public Task Handle(ImportC1ItemActions message, CancellationToken token) =>
        ImportAsync(message.RunId, ItemLookupImportJobValues.ItemActions, Catalog.Actions,
            context => context.ItemActions, context => context.ItemActions,
            item => item.Name, item => item.DisplayName,
            (item, definition) => item.DisplayName = definition.DisplayName,
            (version, definition) => new ItemAction
            {
                GameVersion = version, Name = definition.Name, DisplayName = definition.DisplayName
            }, token);

    public Task Handle(ImportC1ItemBodyParts message, CancellationToken token) =>
        ImportAsync(message.RunId, ItemLookupImportJobValues.ItemBodyParts, Catalog.BodyParts,
            context => context.ItemBodyParts, context => context.ItemBodyParts,
            item => item.Name, item => item.DisplayName,
            (item, definition) => item.DisplayName = definition.DisplayName,
            (version, definition) => new ItemBodyPart
            {
                GameVersion = version, Name = definition.Name, DisplayName = definition.DisplayName
            }, token);

    public Task Handle(ImportC1ItemMaterials message, CancellationToken token) =>
        ImportAsync(message.RunId, ItemLookupImportJobValues.ItemMaterials, Catalog.Materials,
            context => context.ItemMaterials, context => context.ItemMaterials,
            item => item.Name, item => item.DisplayName,
            (item, definition) => item.DisplayName = definition.DisplayName,
            (version, definition) => new ItemMaterial
            {
                GameVersion = version, Name = definition.Name, DisplayName = definition.DisplayName
            }, token);

    public Task Handle(ImportC1ItemCrystalTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, ItemLookupImportJobValues.ItemCrystalTypes, Catalog.CrystalTypes,
            context => context.ItemCrystalTypes, context => context.ItemCrystalTypes,
            item => item.Name, item => item.DisplayName,
            (item, definition) => item.DisplayName = definition.DisplayName,
            (version, definition) => new ItemCrystalType
            {
                GameVersion = version, Name = definition.Name, DisplayName = definition.DisplayName
            }, token);

    private async Task ImportAsync<TEntity>(
        Guid runId,
        string kind,
        IReadOnlyList<ItemLookupDefinition> definitions,
        Func<GameContentDbContext, IQueryable<TEntity>> query,
        Func<GameContentDbContext, DbSet<TEntity>> set,
        Func<TEntity, string> name,
        Func<TEntity, string> displayName,
        Action<TEntity, ItemLookupDefinition> apply,
        Func<string, ItemLookupDefinition, TEntity> create,
        CancellationToken token)
        where TEntity : class
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(token);
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var run = await context.ContentImportRuns.SingleOrDefaultAsync(value =>
                value.Id == runId && value.Kind == kind, token);
            if (run is null || ItemLookupImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            if (!ItemLookupImportJobValues.Supports(run.GameVersion, kind) ||
                !ItemLookupImportJobValues.SupportedModes.Contains(run.Mode))
                throw new InvalidOperationException("Only C1 item lookup imports are supported.");

            run.Status = ItemLookupImportJobValues.Running;
            run.StartedAt ??= timeProvider.GetUtcNow();
            run.LastHeartbeatAt = timeProvider.GetUtcNow();
            var existing = await query(context).Where(item => EF.Property<string>(item, "GameVersion") == run.GameVersion)
                .ToDictionaryAsync(name, StringComparer.Ordinal, token);
            var reconciliation = Reconcile(definitions,
                existing.ToDictionary(item => item.Key, item => displayName(item.Value), StringComparer.Ordinal),
                run.Mode == ItemLookupImportJobValues.RestoreDefaults);
            foreach (var restored in reconciliation.Restored)
                apply(existing[restored.Key], definitions.Single(definition => definition.Name == restored.Key));
            set(context).AddRange(reconciliation.Missing.Select(definition => create(run.GameVersion, definition)));

            run.TotalCount = definitions.Count;
            run.InsertedCount = reconciliation.Missing.Length;
            run.ExistingCount = definitions.Count - reconciliation.Missing.Length;
            run.RestoredCount = reconciliation.Restored.Count;
            run.Status = ItemLookupImportJobValues.Succeeded;
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

    internal static (ItemLookupDefinition[] Missing, Dictionary<string, string> Restored) Reconcile(
        IReadOnlyList<ItemLookupDefinition> definitions,
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

    private async Task MarkFailedAsync(Guid runId, Exception exception, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var run = await context.ContentImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
        if (run is null || ItemLookupImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = ItemLookupImportJobValues.Failed;
        run.Error = exception.ToString()[..Math.Min(exception.ToString().Length, 4000)];
        run.FinishedAt = timeProvider.GetUtcNow();
        run.LastHeartbeatAt = run.FinishedAt;
        await context.SaveChangesAsync(token);
    }
}

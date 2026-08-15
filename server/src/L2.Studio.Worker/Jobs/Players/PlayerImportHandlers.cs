using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Context.Identifiers;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class PlayerImportHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    TimeProvider timeProvider)
{
    private static readonly C1PlayerCatalog Catalog = new();

    public Task Handle(ImportC1PlayerRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, ContentImportTargetValues.PlayerRaces, token);
    public Task Handle(ImportC1PlayerSexes message, CancellationToken token) =>
        ImportAsync(message.RunId, ContentImportTargetValues.PlayerSexes, token);
    public Task Handle(ImportC1PlayerClasses message, CancellationToken token) =>
        ImportAsync(message.RunId, ContentImportTargetValues.PlayerClasses, token);
    public Task Handle(ImportC1PlayerFaces message, CancellationToken token) =>
        ImportAsync(message.RunId, ContentImportTargetValues.PlayerFaces, token);
    public Task Handle(ImportC1PlayerHairStyles message, CancellationToken token) =>
        ImportAsync(message.RunId, ContentImportTargetValues.PlayerHairStyles, token);
    public Task Handle(ImportC1PlayerHairColors message, CancellationToken token) =>
        ImportAsync(message.RunId, ContentImportTargetValues.PlayerHairColors, token);

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
                throw new InvalidOperationException("Only C1 add-missing and restore-defaults player imports are supported.");

            run.Status = ImportJobValues.Running;
            run.StartedAt ??= timeProvider.GetUtcNow();
            run.LastHeartbeatAt = timeProvider.GetUtcNow();
            var restoreDefaults = run.Mode == ImportJobValues.RestoreDefaults;
            var counts = await ImportTargetAsync(context, run.GameVersion, target, restoreDefaults, token);
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

    private static async Task<ImportCounts> ImportTargetAsync(
        GameContentDbContext context,
        string gameVersion,
        string target,
        bool restoreDefaults,
        CancellationToken token)
    {
        if (target is not (ContentImportTargetValues.PlayerRaces or ContentImportTargetValues.PlayerSexes))
            await EnsureDependenciesAsync(context, gameVersion, token);

        return target switch
        {
            ContentImportTargetValues.PlayerRaces => await ImportRacesAsync(context, gameVersion, restoreDefaults, token),
            ContentImportTargetValues.PlayerSexes => await ImportSexesAsync(context, gameVersion, restoreDefaults, token),
            ContentImportTargetValues.PlayerClasses => await ImportClassesAsync(context, gameVersion, restoreDefaults, token),
            ContentImportTargetValues.PlayerFaces => await ImportAppearancesAsync(
                context, gameVersion, Catalog.Faces, context.PlayerFaces,
                definition => new PlayerFace
                {
                    GameVersion = gameVersion, Id = definition.Id, PlayerSexId = definition.SexId,
                    PlayerRaceId = definition.RaceId, Name = definition.Name
                }, restoreDefaults, token),
            ContentImportTargetValues.PlayerHairStyles => await ImportAppearancesAsync(
                context, gameVersion, Catalog.HairStyles, context.PlayerHairStyles,
                definition => new PlayerHairStyle
                {
                    GameVersion = gameVersion, Id = definition.Id, PlayerSexId = definition.SexId,
                    PlayerRaceId = definition.RaceId, Name = definition.Name
                }, restoreDefaults, token),
            ContentImportTargetValues.PlayerHairColors => await ImportAppearancesAsync(
                context, gameVersion, Catalog.HairColors, context.PlayerHairColors,
                definition => new PlayerHairColor
                {
                    GameVersion = gameVersion, Id = definition.Id, PlayerSexId = definition.SexId,
                    PlayerRaceId = definition.RaceId, Name = definition.Name
                }, restoreDefaults, token),
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
    }

    private static async Task EnsureDependenciesAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken token)
    {
        await ImportRacesAsync(context, gameVersion, false, token);
        await ImportSexesAsync(context, gameVersion, false, token);
        await context.SaveChangesAsync(token);
    }

    private static async Task<ImportCounts> ImportRacesAsync(
        GameContentDbContext context, string gameVersion, bool restoreDefaults, CancellationToken token)
    {
        var existing = await context.PlayerRaces.Where(item => item.GameVersion == gameVersion)
            .ToDictionaryAsync(item => item.Id, token);
        var result = Reconcile(Catalog.Races, existing, (entity, definition) => entity.Name = definition.Name,
            definition => new PlayerRace { GameVersion = gameVersion, Id = definition.Id, Name = definition.Name },
            definition => definition.Id, restoreDefaults);
        context.PlayerRaces.AddRange(result.Missing);
        return result.Counts;
    }

    private static async Task<ImportCounts> ImportSexesAsync(
        GameContentDbContext context, string gameVersion, bool restoreDefaults, CancellationToken token)
    {
        var existing = await context.PlayerSexes.Where(item => item.GameVersion == gameVersion)
            .ToDictionaryAsync(item => item.Id, token);
        var result = Reconcile(Catalog.Sexes, existing, (entity, definition) => entity.Name = definition.Name,
            definition => new PlayerSex { GameVersion = gameVersion, Id = definition.Id, Name = definition.Name },
            definition => definition.Id, restoreDefaults);
        context.PlayerSexes.AddRange(result.Missing);
        return result.Counts;
    }

    private static async Task<ImportCounts> ImportClassesAsync(
        GameContentDbContext context, string gameVersion, bool restoreDefaults, CancellationToken token)
    {
        var existing = await context.PlayerClasses.Where(item => item.GameVersion == gameVersion)
            .ToDictionaryAsync(item => (item.Id, item.PlayerSexId, item.PlayerRaceId), token);
        var definitions = (from definition in Catalog.Classes from sex in Catalog.Sexes
            select new PlayerClassVariantDefinition(
                definition.Id, definition.RaceId, definition.IsMage,
                definition.ParentClassId, definition.Name, sex.Id)).ToArray();
        var result = Reconcile(definitions, existing,
            (entity, definition) =>
            {
                entity.Name = definition.Name;
                entity.IsMage = definition.IsMage;
                entity.ParentClassId = definition.ParentClassId;
            },
            definition => new PlayerClass
            {
                GameVersion = gameVersion, Id = definition.Id, PlayerRaceId = definition.RaceId,
                PlayerSexId = definition.SexId, Name = definition.Name, IsMage = definition.IsMage,
                ParentClassId = definition.ParentClassId
            },
            definition => (definition.Id, definition.SexId, definition.RaceId), restoreDefaults);
        context.PlayerClasses.AddRange(result.Missing);
        return result.Counts;
    }

    private static async Task<ImportCounts> ImportAppearancesAsync<TEntity>(
        GameContentDbContext context,
        string gameVersion,
        IReadOnlyList<PlayerAppearanceDefinition> definitions,
        DbSet<TEntity> set,
        Func<PlayerAppearanceDefinition, TEntity> create,
        bool restoreDefaults,
        CancellationToken token)
        where TEntity : class
    {
        var existing = await set.Where(item => EF.Property<string>(item, "GameVersion") == gameVersion)
            .ToDictionaryAsync(item => (
                EF.Property<int>(item, "Id"),
                EF.Property<PlayerSexId>(item, "PlayerSexId"),
                EF.Property<PlayerRaceId>(item, "PlayerRaceId")), token);
        var result = Reconcile(definitions, existing,
            (entity, definition) => context.Entry(entity).Property("Name").CurrentValue = definition.Name,
            create,
            definition => (definition.Id, definition.SexId, definition.RaceId), restoreDefaults);
        set.AddRange(result.Missing);
        return result.Counts;
    }

    private static ReconcileResult<TEntity> Reconcile<TDefinition, TKey, TEntity>(
        IReadOnlyList<TDefinition> definitions,
        IReadOnlyDictionary<TKey, TEntity> existing,
        Action<TEntity, TDefinition> apply,
        Func<TDefinition, TEntity> create,
        Func<TDefinition, TKey> key,
        bool restoreDefaults)
        where TKey : notnull
        where TEntity : class
    {
        var missing = definitions.Where(definition => !existing.ContainsKey(key(definition))).Select(create).ToArray();
        var restored = 0;
        if (restoreDefaults)
        {
            foreach (var definition in definitions)
            {
                if (!existing.TryGetValue(key(definition), out var entity)) continue;
                apply(entity, definition);
                restored++;
            }
        }
        return new ReconcileResult<TEntity>(
            new ImportCounts(definitions.Count, missing.Length, definitions.Count - missing.Length, restored), missing);
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

    private sealed record PlayerClassVariantDefinition(
        PlayerClassId Id, PlayerRaceId RaceId, bool IsMage,
        PlayerClassId? ParentClassId, string Name, PlayerSexId SexId);
    private sealed record ImportCounts(int Total, int Inserted, int Existing, int Restored);
    private sealed record ReconcileResult<TEntity>(ImportCounts Counts, TEntity[] Missing);
}

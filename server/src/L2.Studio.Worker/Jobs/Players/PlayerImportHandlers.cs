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

    public Task Handle(ImportC1Players message, CancellationToken token) => ImportAsync(message.RunId, token);

    private async Task ImportAsync(Guid runId, CancellationToken token)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(token);
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var run = await context.PlayerImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
            if (run is null || PlayerImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            if (run.GameVersion != "c1" || !PlayerImportJobValues.SupportedModes.Contains(run.Mode))
                throw new InvalidOperationException("Only C1 add-missing and restore-defaults player imports are supported.");

            run.Status = PlayerImportJobValues.Running;
            run.StartedAt ??= timeProvider.GetUtcNow();
            var restoreDefaults = run.Mode == PlayerImportJobValues.RestoreDefaults;
            var counts = await ImportCatalogAsync(context, run.GameVersion, restoreDefaults, token);
            run.TotalCount = counts.Total;
            run.InsertedCount = counts.Inserted;
            run.ExistingCount = counts.Existing;
            run.RestoredCount = counts.Restored;
            run.Status = PlayerImportJobValues.Succeeded;
            run.FinishedAt = timeProvider.GetUtcNow();
            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await MarkFailedAsync(runId, exception, token);
        }
    }

    private static async Task<(int Total, int Inserted, int Existing, int Restored)> ImportCatalogAsync(
        GameContentDbContext context, string gameVersion, bool restoreDefaults, CancellationToken token)
    {
        var races = await context.PlayerRaces.Where(item => item.GameVersion == gameVersion)
            .ToDictionaryAsync(item => item.Id, token);
        var sexes = await context.PlayerSexes.Where(item => item.GameVersion == gameVersion)
            .ToDictionaryAsync(item => item.Id, token);
        var classes = await context.PlayerClasses.Where(item => item.GameVersion == gameVersion)
            .ToDictionaryAsync(item => (item.Id, item.PlayerSexId, item.PlayerRaceId), token);
        var faces = await context.PlayerFaces.Where(item => item.GameVersion == gameVersion)
            .ToDictionaryAsync(item => (item.Id, item.PlayerSexId, item.PlayerRaceId), token);
        var hairStyles = await context.PlayerHairStyles.Where(item => item.GameVersion == gameVersion)
            .ToDictionaryAsync(item => (item.Id, item.PlayerSexId, item.PlayerRaceId), token);
        var hairColors = await context.PlayerHairColors.Where(item => item.GameVersion == gameVersion)
            .ToDictionaryAsync(item => (item.Id, item.PlayerSexId, item.PlayerRaceId), token);

        var playerClasses = (from definition in Catalog.Classes from sex in Catalog.Sexes
            select new PlayerClassVariantDefinition(definition.Id, definition.RaceId, definition.IsMage, definition.ParentClassId, definition.Name, sex.Id)).ToArray();
        var raceCounts = Reconcile(Catalog.Races, races, (entity, definition) => entity.Name = definition.Name,
            definition => new PlayerRace { GameVersion = gameVersion, Id = definition.Id, Name = definition.Name },
            definition => definition.Id, restoreDefaults);
        var sexCounts = Reconcile(Catalog.Sexes, sexes, (entity, definition) => entity.Name = definition.Name,
            definition => new PlayerSex { GameVersion = gameVersion, Id = definition.Id, Name = definition.Name },
            definition => definition.Id, restoreDefaults);
        var classCounts = Reconcile(playerClasses, classes,
            (entity, definition) => { entity.Name = definition.Name; entity.IsMage = definition.IsMage; entity.ParentClassId = definition.ParentClassId; },
            definition => new PlayerClass
            {
                GameVersion = gameVersion, Id = definition.Id, PlayerRaceId = definition.RaceId,
                PlayerSexId = definition.SexId, Name = definition.Name, IsMage = definition.IsMage,
                ParentClassId = definition.ParentClassId
            }, definition => (definition.Id, definition.SexId, definition.RaceId), restoreDefaults);
        var faceCounts = Reconcile(Catalog.Faces, faces, (entity, definition) => entity.Name = definition.Name,
            definition => ToEntity(gameVersion, definition, static (version, value) => new PlayerFace
            {
                GameVersion = version, Id = value.Id, PlayerSexId = value.SexId, PlayerRaceId = value.RaceId, Name = value.Name
            }), definition => (definition.Id, definition.SexId, definition.RaceId), restoreDefaults);
        var hairStyleCounts = Reconcile(Catalog.HairStyles, hairStyles, (entity, definition) => entity.Name = definition.Name,
            definition => ToEntity(gameVersion, definition, static (version, value) => new PlayerHairStyle
            {
                GameVersion = version, Id = value.Id, PlayerSexId = value.SexId, PlayerRaceId = value.RaceId, Name = value.Name
            }), definition => (definition.Id, definition.SexId, definition.RaceId), restoreDefaults);
        var hairColorCounts = Reconcile(Catalog.HairColors, hairColors, (entity, definition) => entity.Name = definition.Name,
            definition => ToEntity(gameVersion, definition, static (version, value) => new PlayerHairColor
            {
                GameVersion = version, Id = value.Id, PlayerSexId = value.SexId, PlayerRaceId = value.RaceId, Name = value.Name
            }), definition => (definition.Id, definition.SexId, definition.RaceId), restoreDefaults);

        context.PlayerRaces.AddRange(raceCounts.Missing);
        context.PlayerSexes.AddRange(sexCounts.Missing);
        context.PlayerClasses.AddRange(classCounts.Missing);
        context.PlayerFaces.AddRange(faceCounts.Missing);
        context.PlayerHairStyles.AddRange(hairStyleCounts.Missing);
        context.PlayerHairColors.AddRange(hairColorCounts.Missing);
        return (
            raceCounts.Total + sexCounts.Total + classCounts.Total + faceCounts.Total + hairStyleCounts.Total + hairColorCounts.Total,
            raceCounts.Inserted + sexCounts.Inserted + classCounts.Inserted + faceCounts.Inserted + hairStyleCounts.Inserted + hairColorCounts.Inserted,
            raceCounts.Existing + sexCounts.Existing + classCounts.Existing + faceCounts.Existing + hairStyleCounts.Existing + hairColorCounts.Existing,
            raceCounts.Restored + sexCounts.Restored + classCounts.Restored + faceCounts.Restored + hairStyleCounts.Restored + hairColorCounts.Restored);
    }

    private static TEntity ToEntity<TEntity>(
        string gameVersion,
        PlayerAppearanceDefinition definition,
        Func<string, PlayerAppearanceDefinition, TEntity> create) where TEntity : class =>
        create(gameVersion, definition);

    private static (int Total, int Inserted, int Existing, int Restored, TEntity[] Missing) Reconcile<TDefinition, TKey, TEntity>(
        IReadOnlyList<TDefinition> definitions,
        IReadOnlyDictionary<TKey, TEntity> existing,
        Action<TEntity, TDefinition> apply,
        Func<TDefinition, TEntity> create,
        Func<TDefinition, TKey> key,
        bool restoreDefaults)
        where TKey : notnull
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
        return (definitions.Count, missing.Length, definitions.Count - missing.Length, restored, missing);
    }

    private async Task MarkFailedAsync(Guid runId, Exception exception, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var run = await context.PlayerImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
        if (run is null || PlayerImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = PlayerImportJobValues.Failed;
        run.Error = exception.ToString()[..Math.Min(exception.ToString().Length, 4000)];
        run.FinishedAt = timeProvider.GetUtcNow();
        await context.SaveChangesAsync(token);
    }

    private sealed record PlayerClassVariantDefinition(
        PlayerClassId Id, PlayerRaceId RaceId, bool IsMage, PlayerClassId? ParentClassId, string Name, PlayerSexId SexId);
}

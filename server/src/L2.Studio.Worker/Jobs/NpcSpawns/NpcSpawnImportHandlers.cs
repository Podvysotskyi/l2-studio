using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class NpcSpawnImportHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    TimeProvider timeProvider)
{
    private static readonly C1NpcSpawnCatalog Catalog = new();

    public Task Handle(ImportC1NpcSpawns message, CancellationToken token) => ImportC1Async(message.RunId, token);

    private async Task ImportC1Async(Guid runId, CancellationToken token)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(token);
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var run = await context.ContentImportRuns.SingleOrDefaultAsync(value =>
                value.Id == runId && value.Kind == ContentImportTargetValues.NpcSpawns, token);
            if (run is null || NpcLookupImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            if (run.GameVersion != "c1" || !NpcLookupImportJobValues.SupportedModes.Contains(run.Mode))
                throw new InvalidOperationException("Only C1 add-missing and restore-defaults NPC-spawn imports are supported.");

            var now = timeProvider.GetUtcNow();
            run.Status = NpcLookupImportJobValues.Running;
            run.StartedAt ??= now;
            run.LastHeartbeatAt = now;

            var zones = await context.NpcSpawnZones
                .Include(value => value.Territory).ThenInclude(value => value!.Nodes)
                .Include(value => value.Entities)
                .Where(value => value.GameVersion == run.GameVersion)
                .ToDictionaryAsync(value => value.Name, token);
            var spawns = await context.NpcSpawns.Include(value => value.Entities)
                .Where(value => value.GameVersion == run.GameVersion)
                .ToDictionaryAsync(value => value.Name, token);

            var missingZones = Catalog.Zones.Where(value => !zones.ContainsKey(value.Name)).ToArray();
            var missingSpawns = Catalog.Spawns.Where(value => !spawns.ContainsKey(value.Name)).ToArray();
            context.NpcSpawnZones.AddRange(missingZones.Select(value => ToEntity(run.GameVersion, value)));
            context.NpcSpawns.AddRange(missingSpawns.Select(value => ToEntity(run.GameVersion, value)));

            var restored = 0;
            if (run.Mode == NpcLookupImportJobValues.RestoreDefaults)
            {
                foreach (var definition in Catalog.Zones.Where(value => zones.ContainsKey(value.Name)))
                {
                    Apply(context, zones[definition.Name], definition);
                    restored++;
                }
                foreach (var definition in Catalog.Spawns.Where(value => spawns.ContainsKey(value.Name)))
                {
                    Apply(context, spawns[definition.Name], definition);
                    restored++;
                }
            }

            var total = Catalog.Zones.Count + Catalog.Spawns.Count;
            run.TotalCount = total;
            run.InsertedCount = missingZones.Length + missingSpawns.Length;
            run.ExistingCount = total - run.InsertedCount;
            run.RestoredCount = restored;
            run.Status = NpcLookupImportJobValues.Succeeded;
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

    private static NpcSpawnZone ToEntity(string gameVersion, NpcSpawnZoneDefinition definition)
    {
        var entity = new NpcSpawnZone { GameVersion = gameVersion, Name = definition.Name };
        Apply(null, entity, definition);
        return entity;
    }

    private static NpcSpawn ToEntity(string gameVersion, NpcSpawnDefinition definition)
    {
        var entity = new NpcSpawn { GameVersion = gameVersion, Name = definition.Name };
        Apply(null, entity, definition);
        return entity;
    }

    private static void Apply(GameContentDbContext? context, NpcSpawnZone zone, NpcSpawnZoneDefinition definition)
    {
        var territory = zone.Territory;
        if (territory is null)
        {
            territory = new NpcSpawnZoneTerritory
            {
                GameVersion = zone.GameVersion,
                NpcSpawnZoneName = zone.Name
            };
            zone.Territory = territory;
            if (context is not null) context.NpcSpawnZoneTerritories.Add(territory);
        }
        territory.MinZ = definition.MinZ;
        territory.MaxZ = definition.MaxZ;
        ApplyTerritoryNodes(context, territory, definition.TerritoryNodes);
        ApplyZoneEntities(context, zone, definition.Entities);
    }

    private static void Apply(GameContentDbContext? context, NpcSpawn spawn, NpcSpawnDefinition definition) =>
        ApplySpawnEntities(context, spawn, definition.Entities);

    private static void ApplyTerritoryNodes(
        GameContentDbContext? context,
        NpcSpawnZoneTerritory territory,
        IReadOnlyList<NpcSpawnZoneTerritoryNodeDefinition> definitions)
    {
        var existing = territory.Nodes.ToDictionary(value => value.Sequence);
        var sequences = definitions.Select(value => value.Sequence).ToHashSet();
        foreach (var node in territory.Nodes.Where(value => !sequences.Contains(value.Sequence)).ToArray())
        {
            if (context is not null) context.NpcSpawnZoneTerritoryNodes.Remove(node);
            territory.Nodes.Remove(node);
        }
        foreach (var definition in definitions)
        {
            if (!existing.TryGetValue(definition.Sequence, out var node))
            {
                node = new NpcSpawnZoneTerritoryNode
                {
                    GameVersion = territory.GameVersion,
                    NpcSpawnZoneName = territory.NpcSpawnZoneName,
                    Sequence = definition.Sequence
                };
                territory.Nodes.Add(node);
            }
            node.X = definition.X;
            node.Y = definition.Y;
        }
    }

    private static void ApplyZoneEntities(
        GameContentDbContext? context,
        NpcSpawnZone zone,
        IReadOnlyList<NpcSpawnZoneEntityDefinition> definitions)
    {
        var existing = zone.Entities.ToDictionary(value => value.Sequence);
        var sequences = definitions.Select(value => value.Sequence).ToHashSet();
        foreach (var entity in zone.Entities.Where(value => !sequences.Contains(value.Sequence)).ToArray())
        {
            if (context is not null) context.NpcSpawnZoneEntities.Remove(entity);
            zone.Entities.Remove(entity);
        }
        foreach (var definition in definitions)
        {
            if (!existing.TryGetValue(definition.Sequence, out var entity))
            {
                entity = new NpcSpawnZoneEntity
                {
                    GameVersion = zone.GameVersion,
                    NpcSpawnZoneName = zone.Name,
                    Sequence = definition.Sequence
                };
                zone.Entities.Add(entity);
            }
            entity.NpcId = definition.NpcId;
            entity.Count = definition.Count;
            entity.RespawnDelaySeconds = definition.RespawnDelaySeconds;
            entity.RespawnRandomSeconds = definition.RespawnRandomSeconds;
        }
    }

    private static void ApplySpawnEntities(
        GameContentDbContext? context,
        NpcSpawn spawn,
        IReadOnlyList<NpcSpawnEntityDefinition> definitions)
    {
        var existing = spawn.Entities.ToDictionary(value => value.Sequence);
        var sequences = definitions.Select(value => value.Sequence).ToHashSet();
        foreach (var entity in spawn.Entities.Where(value => !sequences.Contains(value.Sequence)).ToArray())
        {
            if (context is not null) context.NpcSpawnEntities.Remove(entity);
            spawn.Entities.Remove(entity);
        }
        foreach (var definition in definitions)
        {
            if (!existing.TryGetValue(definition.Sequence, out var entity))
            {
                entity = new NpcSpawnEntity
                {
                    GameVersion = spawn.GameVersion,
                    NpcSpawnName = spawn.Name,
                    Sequence = definition.Sequence
                };
                spawn.Entities.Add(entity);
            }
            entity.NpcId = definition.NpcId;
            entity.X = definition.X;
            entity.Y = definition.Y;
            entity.Z = definition.Z;
            entity.Heading = definition.Heading;
            entity.RespawnDelaySeconds = definition.RespawnDelaySeconds;
        }
    }

    private async Task MarkFailedAsync(Guid runId, Exception exception, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var run = await context.ContentImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
        if (run is null || NpcLookupImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = NpcLookupImportJobValues.Failed;
        var error = exception.ToString();
        run.Error = error[..Math.Min(error.Length, 4000)];
        run.FinishedAt = timeProvider.GetUtcNow();
        run.LastHeartbeatAt = run.FinishedAt;
        await context.SaveChangesAsync(token);
    }
}

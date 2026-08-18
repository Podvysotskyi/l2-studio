using L2.Studio.Contracts;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Repositories;

public sealed partial class ContentDirectoryRepository
{
    public async Task<NpcSpawnWorldMap> GetNpcSpawnWorldMapAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var npcNames = await context.Npcs.AsNoTracking()
            .Where(value => value.GameVersion == gameVersion)
            .ToDictionaryAsync(value => value.Id, value => value.Name, cancellationToken);
        var zones = await context.NpcSpawnZones.AsNoTracking()
            .Where(value => value.GameVersion == gameVersion)
            .Include(value => value.Territory)!
                .ThenInclude(value => value!.Nodes)
            .Include(value => value.Entities)
            .OrderBy(value => value.Name)
            .ToArrayAsync(cancellationToken);
        var points = await context.NpcSpawnEntities.AsNoTracking()
            .Where(value => value.GameVersion == gameVersion)
            .OrderBy(value => value.NpcSpawnName)
            .ThenBy(value => value.Sequence)
            .ToArrayAsync(cancellationToken);

        return new NpcSpawnWorldMap(
            zones.Where(value => value.Territory is not null).Select(value => new NpcSpawnWorldMapZone(
                value.Name,
                value.Territory!.MinZ,
                value.Territory.MaxZ,
                value.Territory.Nodes.OrderBy(node => node.Sequence).Select(node =>
                    new NpcSpawnWorldMapTerritoryNode(node.Sequence, node.X, node.Y)).ToArray(),
                value.Entities.OrderBy(entity => entity.Sequence).Select(entity => new NpcSpawnWorldMapZoneNpc(
                    entity.NpcId,
                    npcNames.GetValueOrDefault(entity.NpcId),
                    entity.Count,
                    entity.RespawnDelaySeconds,
                    entity.RespawnRandomSeconds)).ToArray())).ToArray(),
            points.Select(value => new NpcSpawnWorldMapPoint(
                value.NpcSpawnName,
                value.Sequence,
                value.NpcId,
                npcNames.GetValueOrDefault(value.NpcId),
                value.X,
                value.Y,
                value.Z,
                value.Heading,
                value.RespawnDelaySeconds)).ToArray());
    }
}

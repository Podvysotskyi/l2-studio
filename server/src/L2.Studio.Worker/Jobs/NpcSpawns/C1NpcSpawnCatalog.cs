namespace L2.Studio.Worker;

public sealed partial class C1NpcSpawnCatalog
{
    public IReadOnlyList<NpcSpawnZoneDefinition> Zones => ZoneDefinitions;
    public IReadOnlyList<NpcSpawnDefinition> Spawns => SpawnDefinitions;
}

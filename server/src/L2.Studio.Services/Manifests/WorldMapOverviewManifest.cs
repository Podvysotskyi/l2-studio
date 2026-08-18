namespace L2.Studio.Services;

internal sealed record WorldMapOverviewManifest(
    int SchemaVersion,
    int TerrainResolution,
    IReadOnlyList<WorldMapOverviewTerrainTile> Tiles);

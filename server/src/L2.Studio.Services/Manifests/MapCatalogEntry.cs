namespace L2.Studio.Services;

internal sealed record MapCatalogEntry(
    string Name,
    string FileName,
    string? ManifestUrl,
    int TerrainCount,
    int ActorCount,
    int WaterVolumeCount,
    string Sha256,
    string Status,
    string? Error);

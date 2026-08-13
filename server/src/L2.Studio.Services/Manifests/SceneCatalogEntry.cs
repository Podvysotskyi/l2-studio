namespace L2.Studio.Services;

internal sealed record SceneCatalogEntry(
    string Name,
    string FileName,
    string? ManifestUrl,
    int TerrainCount,
    int ActorCount,
    int CinematicObjectCount,
    string Sha256,
    string Status,
    string? Error,
    string SourceKey = "");

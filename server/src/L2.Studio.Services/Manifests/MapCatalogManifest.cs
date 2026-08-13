namespace L2.Studio.Services;

internal sealed record MapCatalogManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<MapCatalogEntry> Maps);

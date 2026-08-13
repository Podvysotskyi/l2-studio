namespace L2.Studio.Services;

internal sealed record SceneCatalogManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<SceneCatalogEntry> Scenes);

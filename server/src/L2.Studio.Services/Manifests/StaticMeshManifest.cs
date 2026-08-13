namespace L2.Studio.Services;

internal sealed record StaticMeshManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<StaticMeshManifestPackage> Packages,
    IReadOnlyList<StaticMeshManifestEntry> Meshes,
    IReadOnlyList<string>? GpuTextureFormats = null);

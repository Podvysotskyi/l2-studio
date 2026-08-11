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

internal sealed record StaticMeshManifestPackage(
    string Name,
    string FileName,
    string Sha256,
    int MeshCount);

internal sealed record StaticMeshManifestEntry(
    string PackageName,
    string ObjectName,
    string? Url,
    int VertexCount,
    int TriangleCount,
    int SectionCount,
    int MaterialCount,
    int ResolvedMaterialCount,
    string MaterialStatus,
    string? MaterialError,
    string? Sha256,
    string Status,
    string? Error);


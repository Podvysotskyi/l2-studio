namespace L2.Studio.Services;

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

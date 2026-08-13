namespace L2.Studio.Services;

internal sealed record MapBspMeshManifestEntry(
    string Name,
    string ModelName,
    string Role,
    string? SkyZone,
    IReadOnlyList<string> WaterVolumeNames,
    string? MeshUrl,
    int VertexCount,
    int TriangleCount,
    int SurfaceCount,
    int MaterialCount,
    int ResolvedMaterialCount,
    string MaterialStatus,
    uint PolyFlags,
    int SplitterNodeCount,
    int InvisibleSurfaceCount,
    int PortalSurfaceCount,
    int FakeBackdropSurfaceCount,
    int MalformedSurfaceCount,
    int UnresolvedMaterialReferenceCount,
    string? Error);

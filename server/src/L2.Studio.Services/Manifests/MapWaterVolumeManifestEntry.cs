namespace L2.Studio.Services;

internal sealed record MapWaterVolumeManifestEntry(
    string Name,
    string ClassName,
    string? BrushName,
    MapVector Location,
    MapRotation Rotation,
    MapVector PrePivot,
    float DrawScale,
    MapVector DrawScale3D,
    string? MeshUrl,
    int VertexCount,
    int TriangleCount,
    string Status,
    string? Error);

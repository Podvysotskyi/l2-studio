namespace L2.Studio.Services;

internal sealed record MapActorManifestEntry(
    string Name,
    string ClassName,
    MapVector Location,
    MapRotation Rotation,
    MapVector PrePivot,
    float DrawScale,
    MapVector DrawScale3D,
    string? MeshPackage,
    string? MeshObject,
    string? MeshUrl,
    MapVertexLightingReference? VertexLighting);

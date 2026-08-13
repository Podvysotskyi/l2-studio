using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;

namespace L2.Studio.Services;

internal sealed record StaticMeshMaterialResolution(
    IReadOnlyList<StaticMeshMaterialBinding?> SectionMaterials,
    int MaterialCount,
    int ResolvedMaterialCount,
    string Status,
    string? Error);

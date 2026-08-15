using L2.Tools.StaticMeshConverter;

namespace L2.Studio.Services;

internal sealed record NpcMaterialReference(
    string Reference,
    string? Url,
    StaticMeshMaterialBinding? Material);

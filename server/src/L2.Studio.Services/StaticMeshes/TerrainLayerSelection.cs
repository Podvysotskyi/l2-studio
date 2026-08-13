using L2.Tools.PackageReader;

namespace L2.Studio.Services;

internal sealed record TerrainLayerSelection(
    IReadOnlyList<UnrealTerrainLayer> Layers,
    IReadOnlyList<int> IgnoredTrailingLayerIndices,
    string? Error);

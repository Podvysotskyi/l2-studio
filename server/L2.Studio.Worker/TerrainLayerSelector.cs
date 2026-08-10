using L2.Tools.PackageReader;

namespace L2.Studio.Worker;

internal sealed record TerrainLayerSelection(
    IReadOnlyList<UnrealTerrainLayer> Layers,
    IReadOnlyList<int> IgnoredTrailingLayerIndices,
    string? Error);

internal static class TerrainLayerSelector
{
    public static TerrainLayerSelection SelectCompletePrefix(
        IReadOnlyList<UnrealTerrainLayer> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        var lastComplete = -1;
        for (var index = layers.Count - 1; index >= 0; index--)
        {
            if (IsComplete(layers[index]))
            {
                lastComplete = index;
                break;
            }
        }

        if (lastComplete < 0)
        {
            return new TerrainLayerSelection(
                [],
                [],
                "Terrain has no complete texture layers.");
        }

        var incompletePrefix = layers
            .Take(lastComplete + 1)
            .Where(layer => !IsComplete(layer))
            .Select(layer => layer.Index)
            .ToArray();
        if (incompletePrefix.Length > 0)
        {
            return new TerrainLayerSelection(
                [],
                [],
                $"Terrain has an incomplete texture layer before a later complete layer: {string.Join(", ", incompletePrefix)}.");
        }

        return new TerrainLayerSelection(
            layers.Take(lastComplete + 1).ToArray(),
            layers.Skip(lastComplete + 1).Select(layer => layer.Index).ToArray(),
            null);
    }

    private static bool IsComplete(UnrealTerrainLayer layer) =>
        layer.Texture is not null && layer.AlphaMap is not null;
}

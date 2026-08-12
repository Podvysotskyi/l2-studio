using L2.Studio.Repositories.Interfaces.Models;

namespace L2.Studio.Services;

internal static class MapPreviewGeneration
{
    public const int RendererVersion = AssetImportSourceHash.MapPreviewRendererVersion;
    public const int Size = 512;

    public static string ComputeSourceHash(string mapCatalogSourceHash) =>
        AssetImportSourceHash.MapPreview(mapCatalogSourceHash);

    public static string? RequestedMapName(string mapsSourcePath, string jobSourcePath)
    {
        var mapsPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(mapsSourcePath));
        var sourcePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(jobSourcePath));
        if (string.Equals(mapsPath, sourcePath, StringComparison.OrdinalIgnoreCase)) return null;
        if (!string.Equals(Path.GetDirectoryName(sourcePath), mapsPath, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(sourcePath), ".unr", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A targeted map-preview job must reference an .unr file in the configured map directory.");
        }

        return Path.GetFileNameWithoutExtension(sourcePath);
    }

    public static bool CanReuse(
        MapPreviewCatalogManifest? previous,
        MapPreviewCatalogEntry? entry,
        MapCatalogEntry map,
        bool imageExists,
        bool force = false) =>
        !force &&
        previous?.RendererVersion == RendererVersion &&
        entry is { Status: "resolved", ImageUrl: not null } &&
        entry.MapSourceHash == map.Sha256 &&
        imageExists;

    public static bool CanCarryForward(MapPreviewCatalogEntry? entry, bool imageExists) =>
        entry is not null &&
        (entry.Status != "resolved" || entry.ImageUrl is not null && imageExists);
}

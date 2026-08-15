using L2.Studio.Repositories.Interfaces.Models;

namespace L2.Studio.Services;

internal static class MapPreviewGeneration
{
    public const int RendererVersion = AssetImportSourceHash.MapPreviewRendererVersion;
    public const int Size = 512;

    public static string ComputeSourceHash(string mapCatalogSourceHash) =>
        AssetImportSourceHash.MapPreview(mapCatalogSourceHash);

    public static string ArtifactFingerprint(
        string sourceHash,
        IEnumerable<(string Kind, string Key, string Fingerprint)> dependencies,
        bool force,
        Guid runId) =>
        AssetArtifactFingerprint.Compute(
            AssetImportJobValues.MapPreviews,
            sourceHash,
            force
                ? dependencies.Append(("preview-refresh", "run", runId.ToString("N")))
                : dependencies);

    public static string? RequestedMapSourceKey(string gameVersionSourcePath, string jobSourcePath)
    {
        var mapsPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameVersionSourcePath));
        var sourcePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(jobSourcePath));
        if (string.Equals(mapsPath, sourcePath, StringComparison.OrdinalIgnoreCase)) return null;
        var relative = Path.GetRelativePath(mapsPath, sourcePath);
        if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal) ||
            !string.Equals(Path.GetExtension(sourcePath), ".unr", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A targeted map-preview job must reference an .unr file in the configured game-version directory.");
        }

        return relative.Replace('\\', '/');
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

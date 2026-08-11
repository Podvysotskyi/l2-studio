using System.Security.Cryptography;
using System.Text;

namespace L2.Studio.Services;

internal static class LevelPreviewGeneration
{
    public const int RendererVersion = 3;
    public const int Size = 512;

    public static string ComputeSourceHash(string levelCatalogSourceHash) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{levelCatalogSourceHash}\n{RendererVersion}")));

    public static string? RequestedLevelName(string levelsSourcePath, string jobSourcePath)
    {
        var levelsPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(levelsSourcePath));
        var sourcePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(jobSourcePath));
        if (string.Equals(levelsPath, sourcePath, StringComparison.OrdinalIgnoreCase)) return null;
        if (!string.Equals(Path.GetDirectoryName(sourcePath), levelsPath, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(sourcePath), ".unr", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A targeted level-preview job must reference an .unr file in the configured level directory.");
        }

        return Path.GetFileNameWithoutExtension(sourcePath);
    }

    public static bool CanReuse(
        LevelPreviewCatalogManifest? previous,
        LevelPreviewCatalogEntry? entry,
        LevelCatalogEntry level,
        bool imageExists,
        bool force = false) =>
        !force &&
        previous?.RendererVersion == RendererVersion &&
        entry is { Status: "resolved", ImageUrl: not null } &&
        entry.LevelSourceHash == level.Sha256 &&
        imageExists;

    public static bool CanCarryForward(LevelPreviewCatalogEntry? entry, bool imageExists) =>
        entry is not null &&
        (entry.Status != "resolved" || entry.ImageUrl is not null && imageExists);
}

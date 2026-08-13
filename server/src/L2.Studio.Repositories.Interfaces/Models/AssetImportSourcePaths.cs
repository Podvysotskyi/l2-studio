using System.Text.RegularExpressions;

namespace L2.Studio.Repositories.Interfaces.Models;

public static partial class AssetImportSourcePaths
{
    [GeneratedRegex("^[0-9]{2}_[0-9]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex WorldMapNamePattern();

    public static string VersionRoot(string sourceRootPath, string gameVersion) => Path.Combine(
        Path.GetFullPath(sourceRootPath),
        gameVersion switch
        {
            "c1" => "C1",
            "c4" => "C4",
            "interlude" => "Interlude",
            _ => throw new ArgumentOutOfRangeException(nameof(gameVersion))
        });

    public static string ExpectedExtension(string kind) => kind switch
    {
        AssetImportJobValues.Textures => ".utx",
        AssetImportJobValues.StaticMeshes => ".usx",
        AssetImportJobValues.Sounds => ".uax",
        AssetImportJobValues.Music => ".ogg",
        AssetImportJobValues.Maps or AssetImportJobValues.MapPreviews or AssetImportJobValues.Scenes => ".unr",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static bool MatchesKind(string kind, string path)
    {
        if (!string.Equals(Path.GetExtension(path), ExpectedExtension(kind), StringComparison.OrdinalIgnoreCase))
            return false;
        var isWorldMap = WorldMapNamePattern().IsMatch(Path.GetFileNameWithoutExtension(path));
        return kind switch
        {
            AssetImportJobValues.Maps or AssetImportJobValues.MapPreviews => isWorldMap,
            AssetImportJobValues.Scenes => !isWorldMap,
            _ => true
        };
    }
}

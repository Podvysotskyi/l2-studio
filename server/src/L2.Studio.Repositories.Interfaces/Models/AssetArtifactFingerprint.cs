using System.Security.Cryptography;
using System.Text;

namespace L2.Studio.Repositories.Interfaces.Models;

public static class AssetArtifactFingerprint
{
    public static string Compute(
        string kind,
        string sourceHash,
        IEnumerable<(string Kind, string Key, string Fingerprint)> dependencies)
    {
        var version = RecipeVersion(kind);
        var inputs = string.Join('\n', dependencies
            .OrderBy(item => item.Kind, StringComparer.Ordinal)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => $"{item.Kind}\0{item.Key}\0{item.Fingerprint}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{kind}\n{version}\n{sourceHash}\n{inputs}")));
    }

    public static string RecipeVersion(string kind) => kind switch
        {
            AssetImportJobValues.Textures => "textures:10:121",
            AssetImportJobValues.StaticMeshes => "staticmeshes:12:112",
            AssetImportJobValues.Animations => "animations:1:111",
            AssetImportJobValues.Sounds => "sounds:3:111",
            AssetImportJobValues.Music => "music:5",
            AssetImportJobValues.Maps => "maps:19:111",
            AssetImportJobValues.Scenes => "scenes:18:111",
            AssetImportJobValues.MapPreviews => $"mappreviews:4:{AssetImportSourceHash.MapPreviewRendererVersion}",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}

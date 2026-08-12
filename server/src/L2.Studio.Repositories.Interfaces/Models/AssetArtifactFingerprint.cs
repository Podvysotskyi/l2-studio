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
            AssetImportJobValues.Textures => "textures:8:121",
            AssetImportJobValues.StaticMeshes => "staticmeshes:9:111",
            AssetImportJobValues.Sounds => "sounds:2:111",
            AssetImportJobValues.Music => "music:2",
            AssetImportJobValues.Maps => "maps:14:111",
            AssetImportJobValues.Scenes => "scenes:13:111",
            AssetImportJobValues.MapPreviews => $"mappreviews:2:{AssetImportSourceHash.MapPreviewRendererVersion}",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}

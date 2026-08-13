using System.Security.Cryptography;
using System.Text;

namespace L2.Studio.Repositories.Interfaces.Models;

public static class AssetImportSourceHash
{
    public const int MapPreviewRendererVersion = 5;

    public static async Task<string> FileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, true);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
    }

    public static string MapPreview(string mapSourceHash) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{mapSourceHash}\n{MapPreviewRendererVersion}")));
}

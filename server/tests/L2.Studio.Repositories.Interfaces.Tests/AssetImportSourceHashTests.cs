using L2.Studio.Repositories.Interfaces.Models;
using Xunit;

namespace L2.Studio.Repositories.Interfaces.Tests;

public sealed class AssetImportSourceHashTests
{
    [Fact]
    public async Task HashesFilesAndDerivesStablePreviewHashes()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "source bytes");
            Assert.Equal(
                64,
                (await AssetImportSourceHash.FileAsync(path, CancellationToken.None)).Length);
            Assert.Equal(
                AssetImportSourceHash.MapPreview("abc"),
                AssetImportSourceHash.MapPreview("abc"));
            Assert.NotEqual(
                AssetImportSourceHash.MapPreview("abc"),
                AssetImportSourceHash.MapPreview("def"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}

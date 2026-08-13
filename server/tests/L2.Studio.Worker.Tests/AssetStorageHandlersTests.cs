using Xunit;

namespace L2.Studio.Worker.Tests;

public sealed class AssetStorageHandlersTests
{
    [Fact]
    public async Task MatchesOnlyPublishedOutputWithFinalFingerprintMarker()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"l2-studio-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputPath);
        try
        {
            Assert.False(await AssetStorageHandlers.MatchesPublishedArtifactAsync(
                outputPath, "final-fingerprint"));

            await File.WriteAllTextAsync(
                Path.Combine(outputPath, ".l2-asset-version"), "preliminary-fingerprint");
            Assert.False(await AssetStorageHandlers.MatchesPublishedArtifactAsync(
                outputPath, "final-fingerprint"));

            await File.WriteAllTextAsync(
                Path.Combine(outputPath, ".l2-asset-version"), "final-fingerprint\n");
            Assert.True(await AssetStorageHandlers.MatchesPublishedArtifactAsync(
                outputPath, "final-fingerprint"));
        }
        finally
        {
            Directory.Delete(outputPath, recursive: true);
        }
    }
}

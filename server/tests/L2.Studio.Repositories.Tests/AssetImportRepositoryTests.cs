using L2.Studio.Repositories;
using Xunit;

namespace L2.Studio.Repositories.Tests;

public sealed class AssetImportRepositoryTests
{
    [Fact]
    public void NormalizesImportSourceKeys()
    {
        Assert.Equal(
            "example.utx",
            AssetImportRepository.NormalizeSourceKey(" Example.UTX "));
        Assert.Equal(
            "17_25.unr",
            AssetImportRepository.NormalizeSourceKey("17_25.UNR"));
    }
}

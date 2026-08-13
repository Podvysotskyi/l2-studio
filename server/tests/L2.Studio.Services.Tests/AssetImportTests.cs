using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class AssetImportTests
{
    [Theory]
    [InlineData("17_25.unr", true)]
    [InlineData("skylevel.unr", false)]
    [InlineData("17_250.unr", false)]
    public void RoutesUnrealPackagesByCoordinateName(string fileName, bool isMap)
    {
        Assert.Equal(isMap, UnrealPackageKindClassifier.IsWorldMap(fileName));
        Assert.Equal(!isMap, UnrealPackageKindClassifier.IsScene(fileName));
    }

    [Fact]
    public void DefinesTypedDiscoveryAndFileCommandsForEverySupportedKind()
    {
        var assembly = typeof(DiscoverTextures).Assembly;
        Assert.Equal(7, assembly.GetTypes().Count(type =>
            !type.IsInterface && typeof(IAssetImportDiscoveryCommand).IsAssignableFrom(type)));
        Assert.Equal(7, assembly.GetTypes().Count(type =>
            !type.IsInterface && typeof(IAssetImportFileCommand).IsAssignableFrom(type)));
    }

    [Fact]
    public void UsesMapAssetKinds()
    {
        Assert.Contains("maps", AssetImportJobValues.SupportedKinds);
        Assert.Contains("mappreviews", AssetImportJobValues.SupportedKinds);
        Assert.DoesNotContain("levels", AssetImportJobValues.SupportedKinds);
        Assert.DoesNotContain("levelpreviews", AssetImportJobValues.SupportedKinds);
    }

}

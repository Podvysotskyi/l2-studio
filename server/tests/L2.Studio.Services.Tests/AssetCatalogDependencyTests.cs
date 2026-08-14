using L2.Studio.Services;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class AssetCatalogDependencyTests
{
    [Fact]
    public void IdentifiesAResourceAsItsOwnDependency()
    {
        Assert.True(AssetImportJobProcessor.IsOwnSourceDependency(
            "staticmeshes",
            "meshes/giran_antaras_s.usx",
            "StaticMeshes",
            "Meshes/Giran_antaras_s.usx"));
    }

    [Fact]
    public void AllowsDependenciesOnOtherResourcesOrKinds()
    {
        Assert.False(AssetImportJobProcessor.IsOwnSourceDependency(
            "staticmeshes",
            "meshes/giran_antaras_s.usx",
            "staticmeshes",
            "meshes/giran_village_s.usx"));
        Assert.False(AssetImportJobProcessor.IsOwnSourceDependency(
            "staticmeshes",
            "meshes/giran_antaras_s.usx",
            "textures",
            "meshes/giran_antaras_s.usx"));
    }
}

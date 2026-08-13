using L2.Studio.Context.Entities;
using L2.Studio.Repositories.Interfaces.Models;
using Xunit;

namespace L2.Studio.Repositories.Tests;

public sealed class AssetCatalogStoreTests
{
    [Fact]
    public void AcceptsPublicationWhoseOutputDirectoryMatchesFinalFingerprint()
    {
        var workItem = WorkItem("preliminary-fingerprint");
        var publication = Publication("final-fingerprint", "versions/c1/Meshes/Mesh/final-fingerprint");

        AssetCatalogStore.ApplyBuildFingerprint(workItem, publication);

        Assert.Equal("final-fingerprint", workItem.ArtifactFingerprint);
    }

    [Fact]
    public void RejectsPublicationWhoseOutputDirectoryUsesPreliminaryFingerprint()
    {
        var publication = Publication("final-fingerprint", "versions/c1/Meshes/Mesh/preliminary-fingerprint");

        var exception = Assert.Throws<InvalidDataException>(() =>
            AssetCatalogStore.ApplyBuildFingerprint(WorkItem(null), publication));

        Assert.Equal("The artifact output directory does not match its build fingerprint.", exception.Message);
    }

    private static AssetCatalogPublication Publication(string buildFingerprint, string outputRoot) => new(
        Guid.NewGuid(),
        "c1",
        "staticmeshes",
        "Meshes/Mesh.usx",
        "meshes/mesh.usx",
        "staticmeshes",
        "source-hash",
        buildFingerprint,
        outputRoot,
        9,
        111,
        [],
        [],
        [],
        [],
        "staticmeshes:10:112",
        "content-hash",
        "{}",
        [],
        DateTimeOffset.UtcNow);

    private static AssetImportWorkItem WorkItem(string? artifactFingerprint) => new()
    {
        ImportKind = "staticmeshes",
        SourceKey = "Meshes/Mesh.usx",
        NormalizedSourceKey = "meshes/mesh.usx",
        SourcePath = "/workspace/sources/C1/Meshes/Mesh.usx",
        ArtifactFingerprint = artifactFingerprint,
        Status = "running"
    };
}

using L2.Studio.Repositories.Interfaces.Models;
using Xunit;

namespace L2.Studio.Worker.Tests;

public sealed class AssetImportFileDiscoveryTests
{
    [Fact]
    public void FindsEachSupportedKindAcrossTheWholeVersionTree()
    {
        using var directory = new TemporaryDirectory();
        Write(directory.Path, "Textures/World.utx");
        Write(directory.Path, "System Textures/UI.UTX");
        Write(directory.Path, "Meshes/Object.usx");
        Write(directory.Path, "Sounds/Ambient.uax");
        Write(directory.Path, "Music/Theme.ogg");
        Write(directory.Path, "Maps/17_25.unr");
        Write(directory.Path, "Animations/Opening.unr");
        Write(directory.Path, "Animations/Character.ukx");

        AssertKeys(directory.Path, AssetImportJobValues.Textures,
            "System Textures/UI.UTX", "Textures/World.utx");
        AssertKeys(directory.Path, AssetImportJobValues.StaticMeshes, "Meshes/Object.usx");
        AssertKeys(directory.Path, AssetImportJobValues.Sounds, "Sounds/Ambient.uax");
        AssertKeys(directory.Path, AssetImportJobValues.Music, "Music/Theme.ogg");
        AssertKeys(directory.Path, AssetImportJobValues.Maps, "Maps/17_25.unr");
        AssertKeys(directory.Path, AssetImportJobValues.Scenes, "Animations/Opening.unr");
    }

    [Fact]
    public void KeepsDuplicateBasenamesAsDistinctVersionRelativeSources()
    {
        using var directory = new TemporaryDirectory();
        Write(directory.Path, "First/Common.utx");
        Write(directory.Path, "Second/Common.utx");

        AssertKeys(directory.Path, AssetImportJobValues.Textures,
            "First/Common.utx", "Second/Common.utx");
    }

    [Fact]
    public void RejectsVersionRelativePathsDuplicatedIgnoringCase()
    {
        using var directory = new TemporaryDirectory();
        Write(directory.Path, "Textures/Common.utx");
        Write(directory.Path, "textures/common.UTX");

        var exception = Assert.Throws<InvalidDataException>(() =>
            AssetImportFileDiscovery.Paths(directory.Path, AssetImportJobValues.Textures));
        Assert.Contains("duplicated ignoring case", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IgnoresSymbolicLinkSources()
    {
        using var directory = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var target = Write(outside.Path, "External.utx");
        Directory.CreateSymbolicLink(Path.Combine(directory.Path, "linked"), outside.Path);
        File.CreateSymbolicLink(Path.Combine(directory.Path, "Linked.utx"), target);

        Assert.Empty(AssetImportFileDiscovery.Paths(directory.Path, AssetImportJobValues.Textures));
    }

    [Fact]
    public void ReportsAMissingGameVersionDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"l2-missing-{Guid.NewGuid():N}");

        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
            AssetImportFileDiscovery.Paths(path, AssetImportJobValues.Textures));
        Assert.Contains(path, exception.Message, StringComparison.Ordinal);
    }

    private static void AssertKeys(string root, string kind, params string[] expected) =>
        Assert.Equal(expected, AssetImportFileDiscovery.Paths(root, kind)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')));

    private static string Write(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "source");
        return path;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"l2-worker-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}

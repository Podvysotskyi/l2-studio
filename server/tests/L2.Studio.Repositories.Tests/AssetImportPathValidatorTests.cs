using L2.Studio.Repositories;
using Xunit;

namespace L2.Studio.Repositories.Tests;

public sealed class AssetImportPathValidatorTests
{
    [Fact]
    public void RejectsInvalidOrEscapingSourceKeys()
    {
        using var directory = new TemporaryDirectory();
        foreach (var fileName in new[]
                 {
                     "",
                     " ",
                     "../Example.utx",
                     "/Example.utx",
                     "nested/../Example.utx",
                     "nested\\..\\Example.utx"
                 })
        {
            Assert.Throws<ArgumentException>(() =>
                AssetImportPathValidator.ResolveContainedFile(
                    directory.Path,
                    fileName,
                    ".utx"));
        }
    }

    [Fact]
    public void ResolvesNestedSourceKeys()
    {
        using var directory = new TemporaryDirectory();
        var nested = System.IO.Path.Combine(directory.Path, "System", "Textures");
        Directory.CreateDirectory(nested);
        var source = System.IO.Path.Combine(nested, "Example.UTX");
        File.WriteAllText(source, "source");

        Assert.Equal(source, AssetImportPathValidator.ResolveContainedFile(
            directory.Path,
            "System/Textures/example.utx",
            ".utx"));
    }

    [Fact]
    public void RejectsTheWrongFileExtension()
    {
        using var directory = new TemporaryDirectory();

        Assert.Throws<ArgumentException>(() =>
            AssetImportPathValidator.ResolveContainedFile(
                directory.Path,
                "Example.usx",
                ".utx"));
    }

    [Fact]
    public void ReportsAMissingSourceDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"l2-missing-{Guid.NewGuid():N}");

        Assert.Throws<DirectoryNotFoundException>(() =>
            AssetImportPathValidator.ResolveContainedFile(path, "Example.utx", ".utx"));
    }

    [Fact]
    public void ReportsAMissingSourceFile()
    {
        using var directory = new TemporaryDirectory();

        var exception = Assert.Throws<FileNotFoundException>(() =>
            AssetImportPathValidator.ResolveContainedFile(
                directory.Path,
                "Missing.utx",
                ".utx"));
        Assert.Equal("Missing.utx", exception.FileName);
    }

    [Fact]
    public void ResolvesTheOnlyCaseInsensitiveMatch()
    {
        using var directory = new TemporaryDirectory();
        var source = System.IO.Path.Combine(directory.Path, "Example.UTX");
        File.WriteAllText(source, "source");

        Assert.Equal(source, AssetImportPathValidator.ResolveContainedFile(
            directory.Path,
            "example.utx",
            ".utx"));
    }

    [Fact]
    public void RejectsSymbolicLinks()
    {
        using var directory = new TemporaryDirectory();
        var target = System.IO.Path.Combine(directory.Path, "Target.utx");
        var link = System.IO.Path.Combine(directory.Path, "Linked.utx");
        File.WriteAllText(target, "source");
        File.CreateSymbolicLink(link, target);

        Assert.Throws<ArgumentException>(() =>
            AssetImportPathValidator.ResolveContainedFile(
                directory.Path,
                "Linked.utx",
                ".utx"));
    }

    [Fact]
    public void RejectsFilesBelowSymbolicLinkDirectories()
    {
        using var directory = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        File.WriteAllText(System.IO.Path.Combine(outside.Path, "Example.utx"), "source");
        Directory.CreateSymbolicLink(System.IO.Path.Combine(directory.Path, "linked"), outside.Path);

        Assert.Throws<ArgumentException>(() =>
            AssetImportPathValidator.ResolveContainedFile(
                directory.Path,
                "linked/Example.utx",
                ".utx"));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"l2-import-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}

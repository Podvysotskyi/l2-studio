using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Tools.AudioConverter;
using L2.Tools.PackageReader;
using L2.Tools.TextureConverter;
using L2.Tools.StaticMeshConverter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace L2.Studio.Services;

public sealed partial class AssetImportJobProcessor
{

    private static string VersionedUrl(
        string sourceFolder,
        string packageName,
        string fileName,
        string hash,
        bool gpuTextureAvailable = true) =>
        $"/{EscapedUrlRoot(sourceFolder)}/{Uri.EscapeDataString(packageName)}/{Uri.EscapeDataString(fileName)}" +
        (gpuTextureAvailable ? string.Empty : "?gpu=none");

    private static string VersionedFileUrl(string sourceFolder, string fileName, string hash) =>
        $"/{EscapedUrlRoot(sourceFolder)}/{Uri.EscapeDataString(fileName)}";

    private static string EscapedUrlRoot(string sourceFolder) => string.Join('/',
        sourceFolder.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

    private static Task SaveProgressAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken) =>
        job.ProcessedCount % 25 == 0
            ? context.SaveChangesAsync(cancellationToken)
            : Task.CompletedTask;

    private async Task PublishCatalogAsync<TGroup, TItem, TMetadata>(
        GameContentDbContext context,
        AssetImportJob job,
        string finalPath,
        string sourceFolder,
        int schemaVersion,
        int? protocol,
        IReadOnlyList<TGroup> groups,
        IReadOnlyList<TItem> items,
        Func<TGroup, string> groupName,
        Func<TItem, string> itemName,
        Func<TItem, string?> itemGroup,
        Func<TItem, string> itemStatus,
        TMetadata metadata,
        CancellationToken cancellationToken)
    {
        await catalogStore.PublishAsync(new AssetCatalogPublication(
            job.Id,
            job.GameVersion,
            job.Kind,
            job.SourceKey,
            job.NormalizedSourceKey,
            job.Kind,
            job.SourceHash!,
            Path.GetRelativePath(Path.GetFullPath(options.Value.AssetRootPath), finalPath).Replace('\\', '/'),
            schemaVersion,
            protocol,
            groups.Select(group => new AssetCatalogPublicationEntry(
                groupName(group), null, null, JsonSerializer.Serialize(group, ManifestJsonOptions))).ToArray(),
            items.Select(item => new AssetCatalogPublicationEntry(
                itemName(item), itemGroup(item), itemStatus(item), JsonSerializer.Serialize(item, ManifestJsonOptions))).ToArray(),
            JsonSerializer.Serialize(metadata, ManifestJsonOptions),
            JsonSerializer.Deserialize<string[]>(job.WarningsJson) ?? [],
            timeProvider.GetUtcNow()), cancellationToken);
    }

    private static string[] SourceFiles(string sourcePath, string extension, string description)
    {
        if (File.Exists(sourcePath))
        {
            if (!string.Equals(Path.GetExtension(sourcePath), extension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"The {description} source must be a {extension} file.");
            return [sourcePath];
        }
        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException($"The configured {description} source does not exist: {sourcePath}");
        return Directory.EnumerateFiles(sourcePath)
            .Where(path => string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static (string FinalPath, string StagingPath, string UrlRoot) OutputPaths(
        string assetRootPath,
        AssetImportJob job)
    {
        var sourceStem = Path.GetFileNameWithoutExtension(job.SourceKey);
        RequireSafeSegment(sourceStem, "source filename");
        if (string.IsNullOrWhiteSpace(job.SourceHash)) throw new InvalidOperationException("The source hash is unavailable.");
        var relative = Path.Combine(job.Kind, sourceStem, job.SourceHash);
        return (
            Path.Combine(assetRootPath, relative),
            Path.Combine(assetRootPath, ".staging", job.Id.ToString("N")),
            Path.Combine("versions", job.GameVersion, relative).Replace('\\', '/'));
    }

    private static async Task<string[]> ActiveCatalogItemJsonAsync(
        GameContentDbContext context,
        string gameVersion,
        string kind,
        CancellationToken cancellationToken) =>
        await context.AssetCatalogItems.AsNoTracking()
            .Where(item => item.Catalog.GameVersion == gameVersion && item.Catalog.Kind == kind && item.Catalog.IsActive)
            .Select(item => item.MetadataJson)
            .ToArrayAsync(cancellationToken);

    private string AssetRoot(AssetImportJob job) => Path.Combine(
        Path.GetFullPath(options.Value.AssetRootPath),
        "versions",
        job.GameVersion);

    private string SourceRoot(AssetImportJob job, string kind) => Path.Combine(
        Path.GetFullPath(options.Value.SourceRootPath),
        job.GameVersion switch
        {
            "c1" => "C1",
            "c4" => "C4",
            "interlude" => "Interlude",
            _ => throw new InvalidOperationException($"Unknown game version '{job.GameVersion}'.")
        },
        kind is AssetImportJobValues.Levels or AssetImportJobValues.LevelPreviews or AssetImportJobValues.Scenes
            ? "maps"
            : kind);

    private static void Promote(string stagingPath, string finalPath)
    {
        if (Directory.Exists(finalPath))
        {
            if (File.Exists(Path.Combine(finalPath, ".l2-asset-version")))
            {
                Directory.Delete(stagingPath, recursive: true);
                return;
            }
            Directory.Delete(finalPath, recursive: true);
        }
        Directory.Move(stagingPath, finalPath);
    }

    private sealed record PackageSource(
        string Path,
        string Name,
        string FileName,
        string Sha256,
        int TextureCount,
        int MaterialCount);

    private sealed record MusicSource(string Path, string FileName, string Sha256);

    private sealed record StaticMeshPackageSource(
        string Path,
        string Name,
        string FileName,
        string Sha256,
        int MeshCount);

    private sealed record LevelSource(string Path, string Name, string FileName, string Sha256);

    private sealed record TerrainMaterialBuild(
        IReadOnlyList<LevelTerrainLayerManifestEntry> Layers,
        IReadOnlyList<string> ControlMapUrls,
        int ControlMapWidth,
        int ControlMapHeight,
        string? Error,
        string? Warning = null);

    private sealed record PublishedTexture(string Url, int Width, int Height);

    private sealed record StaticMeshCatalogMetadata(IReadOnlyList<string> GpuTextureFormats);

    private sealed record LevelPreviewCatalogMetadata(int RendererVersion);

    private sealed record StaticMeshLookup(
        IReadOnlyDictionary<string, PublishedStaticMesh> Meshes,
        IReadOnlyList<string> GpuTextureFormats);

    private sealed record PublishedStaticMesh(string Url, int VertexCount);

    private sealed record TextureConversionResult(
        UnrealTextureExport Export,
        byte[]? Image,
        string? ImageHash,
        byte[]? GpuImage,
        string? GpuImageHash,
        string? VersionHash,
        string? Error,
        bool IsWarning);
}

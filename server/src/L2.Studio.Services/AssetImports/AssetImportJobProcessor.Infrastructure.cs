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
        $"/{Uri.EscapeDataString(sourceFolder)}/{Uri.EscapeDataString(packageName)}/{Uri.EscapeDataString(fileName)}" +
        $"?v={hash[..12]}{(gpuTextureAvailable ? string.Empty : "&gpu=none")}";

    private static string VersionedFileUrl(string sourceFolder, string fileName, string hash) =>
        $"/{Uri.EscapeDataString(sourceFolder)}/{Uri.EscapeDataString(fileName)}?v={hash[..12]}";

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
        var backupPath = $"{finalPath}.backup-{job.Id:N}";
        try
        {
            await catalogStore.PublishAsync(new AssetCatalogPublication(
                job.Id,
                job.Kind,
                sourceFolder,
                job.SourceHash!,
                schemaVersion,
                protocol,
                groups.Select(group => new AssetCatalogPublicationEntry(
                    groupName(group), null, null, JsonSerializer.Serialize(group, ManifestJsonOptions))).ToArray(),
                items.Select(item => new AssetCatalogPublicationEntry(
                    itemName(item), itemGroup(item), itemStatus(item), JsonSerializer.Serialize(item, ManifestJsonOptions))).ToArray(),
                JsonSerializer.Serialize(metadata, ManifestJsonOptions),
                timeProvider.GetUtcNow()), cancellationToken);
            if (Directory.Exists(backupPath)) Directory.Delete(backupPath, recursive: true);
        }
        catch
        {
            if (Directory.Exists(finalPath)) Directory.Delete(finalPath, recursive: true);
            if (File.Exists(Path.Combine(backupPath, ".empty")))
                Directory.Delete(backupPath, recursive: true);
            else if (Directory.Exists(backupPath))
                Directory.Move(backupPath, finalPath);
            throw;
        }
    }

    private static async Task<string[]> ActiveCatalogItemJsonAsync(
        GameContentDbContext context,
        string kind,
        CancellationToken cancellationToken) =>
        await context.AssetCatalogItems.AsNoTracking()
            .Where(item => item.Catalog.Kind == kind && item.Catalog.IsActive)
            .Select(item => item.MetadataJson)
            .ToArrayAsync(cancellationToken);

    private static void Promote(string stagingPath, string finalPath, Guid jobId)
    {
        var backupPath = $"{finalPath}.backup-{jobId:N}";
        if (Directory.Exists(finalPath))
        {
            Directory.Move(finalPath, backupPath);
        }
        else
        {
            Directory.CreateDirectory(backupPath);
            File.WriteAllText(Path.Combine(backupPath, ".empty"), string.Empty);
        }

        try
        {
            Directory.Move(stagingPath, finalPath);
        }
        catch
        {
            if (!Directory.Exists(finalPath) && Directory.Exists(backupPath))
            {
                if (File.Exists(Path.Combine(backupPath, ".empty")))
                    Directory.Delete(backupPath, recursive: true);
                else
                    Directory.Move(backupPath, finalPath);
            }

            throw;
        }
    }

    private async Task ReconcilePromotionsAsync(
        GameContentDbContext context,
        CancellationToken cancellationToken)
    {
        var assetRootPath = Path.GetFullPath(options.Value.AssetRootPath);
        if (!Directory.Exists(assetRootPath)) return;
        foreach (var backupPath in Directory.EnumerateDirectories(assetRootPath, "*.backup-*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var separator = backupPath.LastIndexOf(".backup-", StringComparison.Ordinal);
            if (separator < 0 || !Guid.TryParseExact(backupPath[(separator + 8)..], "N", out var jobId)) continue;
            var finalPath = backupPath[..separator];
            var published = await context.AssetCatalogs.AsNoTracking()
                .AnyAsync(catalog => catalog.Id == jobId && catalog.IsActive, cancellationToken);
            if (published)
            {
                Directory.Delete(backupPath, recursive: true);
                continue;
            }

            if (Directory.Exists(finalPath)) Directory.Delete(finalPath, recursive: true);
            if (File.Exists(Path.Combine(backupPath, ".empty")))
                Directory.Delete(backupPath, recursive: true);
            else
                Directory.Move(backupPath, finalPath);
            logger.LogWarning("Recovered interrupted asset promotion for job {JobId}", jobId);
        }
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

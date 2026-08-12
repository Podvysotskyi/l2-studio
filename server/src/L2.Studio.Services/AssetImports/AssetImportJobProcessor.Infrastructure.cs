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
using System.Text.RegularExpressions;

namespace L2.Studio.Services;

public sealed partial class AssetImportJobProcessor
{
    private static readonly Regex MissingDependencyPattern = new(
        "(?<type>Texture|Material|Static mesh|Sound) '(?<key>[^']+)' (?:is not published|is unavailable)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
        var publicationGroups = groups.Select(group => new AssetCatalogPublicationEntry(
            groupName(group), null, null, JsonSerializer.Serialize(group, ManifestJsonOptions))).ToArray();
        var publicationItems = items.Select(item => new AssetCatalogPublicationEntry(
            itemName(item), itemGroup(item), itemStatus(item), JsonSerializer.Serialize(item, ManifestJsonOptions))).ToArray();
        var metadataJson = JsonSerializer.Serialize(metadata, ManifestJsonOptions);
        var dependencies = await ResolveDependenciesAsync(
            context,
            job,
            finalPath,
            publicationGroups.Select(item => item.MetadataJson)
                .Concat(publicationItems.Select(item => item.MetadataJson)).Append(metadataJson),
            JsonSerializer.Deserialize<string[]>(job.WarningsJson) ?? [],
            cancellationToken);
        var fingerprint = AssetArtifactFingerprint.Compute(job.Kind, job.SourceHash!, dependencies.Select(dependency => (
            dependency.Kind, dependency.DependencyKey, dependency.ArtifactFingerprint ?? "missing")));
        (finalPath, sourceFolder, publicationGroups, publicationItems, metadataJson) = RelocateArtifact(
            job, finalPath, sourceFolder, fingerprint, publicationGroups, publicationItems, metadataJson);
        job.ArtifactFingerprint = fingerprint;
        await File.WriteAllTextAsync(Path.Combine(finalPath, ".l2-asset-version"), fingerprint, cancellationToken);
        var files = await InventoryFilesAsync(finalPath, sourceFolder, cancellationToken);
        var contentHash = AggregateContentHash(files);
        await File.WriteAllTextAsync(
            Path.Combine(finalPath, ".l2-artifact.json"),
            JsonSerializer.Serialize(new
            {
                gameVersion = job.GameVersion,
                kind = job.Kind,
                sourceKey = job.SourceKey,
                sourceHash = job.SourceHash,
                recipeVersion = AssetArtifactFingerprint.RecipeVersion(job.Kind),
                buildFingerprint = fingerprint,
                contentHash,
                files
            }, ManifestJsonOptions),
            cancellationToken);
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
            publicationGroups,
            publicationItems,
            dependencies,
            files,
            AssetArtifactFingerprint.RecipeVersion(job.Kind),
            contentHash,
            metadataJson,
            JsonSerializer.Deserialize<string[]>(job.WarningsJson) ?? [],
            timeProvider.GetUtcNow()), cancellationToken);
    }

    private async Task<AssetCatalogDependencyPublication[]> ResolveDependenciesAsync(
        GameContentDbContext context,
        AssetImportJob job,
        string finalPath,
        IEnumerable<string> jsonValues,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        var artifactJson = Directory.Exists(finalPath)
            ? Directory.EnumerateFiles(finalPath, "*.json", SearchOption.AllDirectories)
                .Select(File.ReadAllText)
            : [];
        var combined = string.Join('\n', jsonValues.Concat(artifactJson));
        var sources = await context.AssetCatalogSources.AsNoTracking()
            .Where(source => source.Catalog.GameVersion == job.GameVersion && source.Catalog.IsActive)
            .Select(source => new
            {
                source.Catalog.Kind, source.SourceKey, source.NormalizedSourceKey,
                source.ArtifactFingerprint, source.SourceHash, source.OutputRoot
            }).ToArrayAsync(cancellationToken);
        var dependencies = sources.Where(source => combined.Contains('/' + source.OutputRoot + '/', StringComparison.Ordinal))
            .Select(source => new AssetCatalogDependencyPublication(
                source.Kind, source.NormalizedSourceKey, source.SourceKey,
                source.ArtifactFingerprint ?? source.SourceHash, true, source.OutputRoot))
            .ToList();
        dependencies.AddRange(dependencyHints);
        if (job.Kind == AssetImportJobValues.MapPreviews)
        {
            var map = sources.FirstOrDefault(source => source.Kind == AssetImportJobValues.Maps &&
                source.NormalizedSourceKey == job.NormalizedSourceKey);
            if (map is not null)
            {
                dependencies.Add(new AssetCatalogDependencyPublication(
                    map.Kind, map.NormalizedSourceKey, map.SourceKey,
                    map.ArtifactFingerprint ?? map.SourceHash, true, map.OutputRoot));
            }
        }
        foreach (var warning in warnings)
        {
            foreach (Match match in MissingDependencyPattern.Matches(warning))
            {
                var type = match.Groups["type"].Value.ToLowerInvariant();
                var kind = type switch
                {
                    "static mesh" => AssetImportJobValues.StaticMeshes,
                    "sound" => AssetImportJobValues.Sounds,
                    _ => AssetImportJobValues.Textures
                };
                var key = match.Groups["key"].Value.Trim().ToLowerInvariant();
                if (!dependencies.Any(item => item.Kind == kind && item.DependencyKey == key))
                    dependencies.Add(new AssetCatalogDependencyPublication(kind, key, null, null, false, null));
            }
        }
        return dependencies.GroupBy(item => (item.Kind, item.DependencyKey), StringTupleComparer.Instance)
            .Select(group => group.OrderByDescending(item => item.IsResolved).First())
            .OrderBy(item => item.Kind).ThenBy(item => item.DependencyKey).ToArray();
    }

    private async Task TrackTextureDependenciesAsync(
        GameContentDbContext context,
        string gameVersion,
        IEnumerable<TextureMaterialReference> references,
        CancellationToken cancellationToken)
    {
        var requested = references.Select(reference => (
                Package: reference.PackageName.Trim().ToLowerInvariant(),
                Key: $"{reference.PackageName}.{reference.ObjectName}".Trim().ToLowerInvariant()))
            .Distinct().ToArray();
        if (requested.Length == 0) return;
        var packages = requested.Select(item => item.Package).Distinct().ToArray();
        var sources = await context.AssetCatalogSources.AsNoTracking().Include(source => source.Groups)
            .Where(source => source.Catalog.GameVersion == gameVersion && source.Catalog.IsActive &&
                source.Catalog.Kind == AssetImportJobValues.Textures &&
                source.Groups.Any(group => packages.Contains(group.Name.ToLower())))
            .ToArrayAsync(cancellationToken);
        foreach (var reference in requested)
        {
            var source = sources.FirstOrDefault(candidate => candidate.Groups.Any(group =>
                string.Equals(group.Name, reference.Package, StringComparison.OrdinalIgnoreCase)));
            dependencyHints.Add(source is null
                ? new AssetCatalogDependencyPublication(AssetImportJobValues.Textures, reference.Key, null, null, false, null)
                : new AssetCatalogDependencyPublication(AssetImportJobValues.Textures, reference.Key,
                    source.NormalizedSourceKey, source.ArtifactFingerprint ?? source.SourceHash, true, source.OutputRoot));
        }
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string Kind, string DependencyKey)>
    {
        public static readonly StringTupleComparer Instance = new();
        public bool Equals((string Kind, string DependencyKey) x, (string Kind, string DependencyKey) y) =>
            string.Equals(x.Kind, y.Kind, StringComparison.Ordinal) &&
            string.Equals(x.DependencyKey, y.DependencyKey, StringComparison.Ordinal);
        public int GetHashCode((string Kind, string DependencyKey) value) => HashCode.Combine(value.Kind, value.DependencyKey);
    }

    private (string FinalPath, string SourceFolder, AssetCatalogPublicationEntry[] Groups,
        AssetCatalogPublicationEntry[] Items, string MetadataJson) RelocateArtifact(
        AssetImportJob job,
        string finalPath,
        string sourceFolder,
        string fingerprint,
        AssetCatalogPublicationEntry[] groups,
        AssetCatalogPublicationEntry[] items,
        string metadataJson)
    {
        var sourceStem = Path.GetFileNameWithoutExtension(job.SourceKey);
        var sourceDirectory = Path.GetDirectoryName(job.SourceKey);
        var relative = string.IsNullOrEmpty(sourceDirectory)
            ? Path.Combine(job.Kind, sourceStem, fingerprint)
            : Path.Combine(job.Kind, sourceDirectory, sourceStem, fingerprint);
        var target = Path.Combine(AssetRoot(job), relative);
        var targetFolder = Path.Combine("versions", job.GameVersion, relative).Replace('\\', '/');
        if (string.Equals(finalPath, target, StringComparison.Ordinal))
            return (finalPath, sourceFolder, groups, items, metadataJson);
        foreach (var path in Directory.EnumerateFiles(finalPath, "*.json", SearchOption.AllDirectories))
            File.WriteAllText(path, File.ReadAllText(path).Replace(sourceFolder, targetFolder, StringComparison.Ordinal));
        if (Directory.Exists(target))
        {
            if (!string.Equals(DirectoryContentHash(finalPath), DirectoryContentHash(target), StringComparison.Ordinal))
                throw new InvalidDataException(
                    "The converter produced different output for an existing build fingerprint; increment its recipe version.");
            Directory.Delete(finalPath, recursive: true);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            Directory.Move(finalPath, target);
        }
        AssetCatalogPublicationEntry Replace(AssetCatalogPublicationEntry entry) =>
            entry with { MetadataJson = entry.MetadataJson.Replace(sourceFolder, targetFolder, StringComparison.Ordinal) };
        return (target, targetFolder, groups.Select(Replace).ToArray(), items.Select(Replace).ToArray(),
            metadataJson.Replace(sourceFolder, targetFolder, StringComparison.Ordinal));
    }

    private static async Task<AssetArtifactFilePublication[]> InventoryFilesAsync(
        string root,
        string publicRoot,
        CancellationToken cancellationToken)
    {
        var files = new List<AssetArtifactFilePublication>();
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                     .Where(path => !Path.GetFileName(path).StartsWith(".l2-", StringComparison.Ordinal))
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            await using var stream = File.OpenRead(path);
            var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
            var size = new FileInfo(path).Length;
            files.Add(new AssetArtifactFilePublication(
                relative,
                $"/{publicRoot.Trim('/')}/{string.Join('/', relative.Split('/').Select(Uri.EscapeDataString))}",
                FileRole(relative),
                MediaType(relative),
                size,
                hash));
        }
        return files.ToArray();
    }

    private static string AggregateContentHash(IEnumerable<AssetArtifactFilePublication> files)
    {
        var value = string.Join('\n', files.OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .Select(file => $"{file.RelativePath}\0{file.SizeBytes}\0{file.Sha256}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string DirectoryContentHash(string root)
    {
        var values = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).StartsWith(".l2-", StringComparison.Ordinal))
            .OrderBy(path => Path.GetRelativePath(root, path), StringComparer.Ordinal)
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return $"{Path.GetRelativePath(root, path).Replace('\\', '/')}\0{new FileInfo(path).Length}\0" +
                    Convert.ToHexStringLower(SHA256.HashData(stream));
            });
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', values))));
    }

    private static string FileRole(string path) => Path.GetFileName(path).Equals("manifest.json", StringComparison.OrdinalIgnoreCase)
        ? "manifest"
        : Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".webp" or ".png" or ".jpg" or ".jpeg" or ".ktx2" => "texture",
            ".glb" => "mesh",
            ".ogg" => "audio",
            ".json" => "metadata",
            _ => "asset"
        };

    private static string MediaType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".json" => "application/json",
        ".webp" => "image/webp",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".ktx2" => "image/ktx2",
        ".glb" => "model/gltf-binary",
        ".ogg" => "audio/ogg",
        _ => "application/octet-stream"
    };

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

    private (string FinalPath, string StagingPath, string UrlRoot) OutputPaths(
        string assetRootPath,
        AssetImportJob job)
    {
        var sourceStem = Path.GetFileNameWithoutExtension(job.SourceKey);
        var sourceDirectory = Path.GetDirectoryName(job.SourceKey);
        RequireSafeSegment(sourceStem, "source filename");
        if (!string.IsNullOrEmpty(sourceDirectory))
        {
            foreach (var segment in sourceDirectory.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
                RequireSafeSegment(segment, "source folder");
        }
        if (string.IsNullOrWhiteSpace(job.ArtifactFingerprint)) throw new InvalidOperationException("The artifact fingerprint is unavailable.");
        var relative = string.IsNullOrEmpty(sourceDirectory)
            ? Path.Combine(job.Kind, sourceStem, job.ArtifactFingerprint)
            : Path.Combine(job.Kind, sourceDirectory, sourceStem, job.ArtifactFingerprint);
        return (
            Path.Combine(assetRootPath, relative),
            Path.Combine(AssetWorkRoot(job), job.Id.ToString("N")),
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

    private string AssetWorkRoot(AssetImportJob job) =>
        Path.GetFullPath(options.Value.AssetWorkRootPath);

    private string SourceRoot(AssetImportJob job, string kind) => Path.Combine(
        Path.GetFullPath(options.Value.SourceRootPath),
        job.GameVersion switch
        {
            "c1" => "C1",
            "c4" => "C4",
            "interlude" => "Interlude",
            _ => throw new InvalidOperationException($"Unknown game version '{job.GameVersion}'.")
        },
        SourceKindFolder(kind));

    internal static string SourceKindFolder(string kind) => kind switch
    {
        AssetImportJobValues.Textures => "textures",
        AssetImportJobValues.StaticMeshes => "staticmeshes",
        AssetImportJobValues.Sounds => "sounds",
        AssetImportJobValues.Music => "music",
        AssetImportJobValues.Maps or AssetImportJobValues.MapPreviews => "maps",
        AssetImportJobValues.Scenes => "scenes",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

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
        int MaterialCount,
        string OriginalFolder);

    private sealed record MusicSource(string Path, string FileName, string Sha256);

    private sealed record StaticMeshPackageSource(
        string Path,
        string Name,
        string FileName,
        string Sha256,
        int MeshCount);

    private sealed record MapSource(string Path, string Name, string FileName, string Sha256);

    private sealed record TerrainMaterialBuild(
        IReadOnlyList<MapTerrainLayerManifestEntry> Layers,
        IReadOnlyList<string> ControlMapUrls,
        int ControlMapWidth,
        int ControlMapHeight,
        string? Error,
        string? Warning = null);

    private sealed record PublishedTexture(string Url, int Width, int Height);

    private sealed record StaticMeshCatalogMetadata(IReadOnlyList<string> GpuTextureFormats);

    private sealed record MapPreviewCatalogMetadata(int RendererVersion);

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

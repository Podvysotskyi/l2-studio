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
    private async Task ImportTexturesAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken)
    {
        var sourcePath = Path.GetFullPath(job.ConversionSourcePath ?? job.SourcePath);
        var assetRootPath = AssetRoot(job);
        var packagePaths = SourceFiles(sourcePath, ".utx", "texture");
        if (packagePaths.Length == 0)
        {
            throw new InvalidOperationException("The configured system-texture directory contains no .utx packages.");
        }

        var packages = new List<PackageSource>(packagePaths.Length);
        foreach (var packagePath in packagePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(packagePath);
            var packageName = Path.GetFileNameWithoutExtension(fileName);
            RequireSafeSegment(packageName, "package name");
            var encrypted = await File.ReadAllBytesAsync(packagePath, cancellationToken);
            var fileHash = Convert.ToHexStringLower(SHA256.HashData(encrypted));
            var decoded = LineagePackageDecoder.DecodeProtocol121(encrypted, fileName);
            var exports = new UnrealPackageReader(decoded).ReadTextureExports();
            var materials = new UnrealPackageReader(decoded).ReadMaterialExports();
            EnsureUniqueObjectNames(packageName, exports);
            foreach (var export in exports)
            {
                RequireSafeSegment(export.Name, "texture object name");
            }
            packages.Add(new PackageSource(
                packagePath,
                packageName,
                fileName,
                fileHash,
                exports.Count,
                materials.Count));
            job.TotalCount += exports.Count;
        }

        var duplicatePackage = packages
            .GroupBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicatePackage is not null)
        {
            throw new InvalidDataException($"Package name '{duplicatePackage.Key}' is duplicated ignoring case.");
        }

        job.SourceHash = packages.Single().Sha256;
        await context.SaveChangesAsync(cancellationToken);

        Directory.CreateDirectory(assetRootPath);
        var (finalPath, stagingPath, sourceFolder) = OutputPaths(assetRootPath, job);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        Directory.CreateDirectory(stagingPath);
        try
        {
            var entries = new List<TextureManifestEntry>(job.TotalCount);
            var materialEntries = new List<TextureMaterialManifestEntry>();
            var warnings = new List<string>();
            foreach (var package in packages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var encrypted = await File.ReadAllBytesAsync(package.Path, cancellationToken);
                var decoded = LineagePackageDecoder.DecodeProtocol121(encrypted, package.FileName);
                var exports = new UnrealPackageReader(decoded).ReadTextureExports();
                var materials = new UnrealPackageReader(decoded).ReadMaterialExports();
                var packageOutputPath = Path.Combine(stagingPath, package.Name);
                Directory.CreateDirectory(packageOutputPath);

                var orderedExports = exports.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
                var parallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 8));
                foreach (var batch in orderedExports.Chunk(parallelism))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var export in batch)
                    {
                        RequireSafeSegment(export.Name, "texture object name");
                    }

                    var results = await Task.WhenAll(batch.Select(export =>
                        Task.Run(() => ConvertTextureAsync(export, cancellationToken), cancellationToken)));
                    foreach (var result in results)
                    {
                        var export = result.Export;
                        if (result.Image is not null)
                        {
                            var fileName = $"{export.Name}.webp";
                            await File.WriteAllBytesAsync(
                                Path.Combine(packageOutputPath, fileName),
                                result.Image,
                                cancellationToken);
                            string? gpuFileName = null;
                            if (result.GpuImage is not null)
                            {
                                gpuFileName = $"{export.Name}-dxt.ktx";
                                await File.WriteAllBytesAsync(
                                    Path.Combine(packageOutputPath, gpuFileName),
                                    result.GpuImage,
                                    cancellationToken);
                            }
                            var texture = export.Texture!;
                            entries.Add(new TextureManifestEntry(
                                package.Name,
                                export.Name,
                                VersionedUrl(
                                    sourceFolder,
                                    package.Name,
                                    fileName,
                                    result.VersionHash!,
                                    result.GpuImage is not null),
                                texture.Width,
                                texture.Height,
                                FormatName(texture.Format),
                                result.ImageHash,
                                "resolved",
                                null,
                                gpuFileName is null
                                    ? null
                                    : VersionedUrl(
                                        sourceFolder,
                                        package.Name,
                                        gpuFileName,
                                        result.VersionHash!),
                                result.GpuImageHash,
                                result.GpuImage is not null,
                                texture.MipLevels.Count));
                        }
                        else
                        {
                            if (result.IsWarning)
                            {
                                warnings.Add($"{package.FileName}/{export.Name}: {result.Error}");
                            }
                            entries.Add(new TextureManifestEntry(
                                package.Name,
                                export.Name,
                                null,
                                export.Width,
                                export.Height,
                                FormatName(export.Format),
                                null,
                                "skipped",
                                result.Error,
                                MipCount: export.MipCount));
                            job.SkippedCount++;
                        }

                        job.ProcessedCount++;
                        await SaveProgressAsync(context, job, cancellationToken);
                    }
                }

                ApplyTextureAnimations(package.Name, exports, entries, warnings);

                materialEntries.AddRange(materials.Select(material =>
                    MaterialManifest(package.Name, material)));
            }

            var catalogGroups = packages.Select(package => new TextureManifestPackage(
                    package.Name,
                    package.FileName,
                    package.Sha256,
                    package.TextureCount,
                    package.MaterialCount)).ToArray();

            job.WarningsJson = JsonSerializer.Serialize(warnings);
            await File.WriteAllTextAsync(Path.Combine(stagingPath, ".l2-asset-version"), job.SourceHash, cancellationToken);
            Promote(stagingPath, finalPath);
            await PublishCatalogAsync(context, job, finalPath, sourceFolder, 7, 121, catalogGroups, entries,
                group => group.Name, item => item.ObjectName, item => item.PackageName, item => item.Status,
                new TextureCatalogMetadata(materialEntries), cancellationToken);
            job.Status = warnings.Count == 0
                ? AssetImportJobValues.Succeeded
                : AssetImportJobValues.SucceededWithWarnings;
            job.FinishedAt = timeProvider.GetUtcNow();
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Imported {ProcessedCount} textures from {PackageCount} packages with {SkippedCount} skipped for job {JobId}",
                job.ProcessedCount,
                packages.Count,
                job.SkippedCount,
                job.Id);
        }
        finally
        {
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, recursive: true);
            }
        }
    }

    private static void ApplyTextureAnimations(
        string packageName,
        IReadOnlyList<UnrealTextureExport> exports,
        List<TextureManifestEntry> entries,
        List<string> warnings)
    {
        var exportsByName = exports.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        var entryIndices = entries
            .Select((entry, index) => (entry, index))
            .Where(item => string.Equals(item.entry.PackageName, packageName, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(item => item.entry.ObjectName, item => item.index, StringComparer.OrdinalIgnoreCase);

        foreach (var export in exports.Where(item => item.AnimationNext is not null))
        {
            if (!entryIndices.TryGetValue(export.Name, out var entryIndex)) continue;
            var frameNames = new List<string> { export.Name };
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { export.Name };
            var current = export;
            var valid = true;
            while (current.AnimationNext is { } next && frameNames.Count < 256)
            {
                var nextPackage = string.IsNullOrEmpty(next.PackageName) ? packageName : next.PackageName;
                if (!string.Equals(nextPackage, packageName, StringComparison.OrdinalIgnoreCase) ||
                    !exportsByName.TryGetValue(next.ObjectName, out var nextExport))
                {
                    warnings.Add($"{packageName}/{export.Name}: animation frame '{next.Path}' is unavailable.");
                    valid = false;
                    break;
                }
                if (!visited.Add(nextExport.Name))
                {
                    if (!string.Equals(nextExport.Name, export.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.Add($"{packageName}/{export.Name}: animation chain joins a different cycle at '{nextExport.Name}'.");
                        valid = false;
                    }
                    break;
                }
                frameNames.Add(nextExport.Name);
                current = nextExport;
            }
            if (frameNames.Count >= 256)
            {
                warnings.Add($"{packageName}/{export.Name}: animation exceeds 256 frames.");
                valid = false;
            }
            if (!valid || frameNames.Count < 2) continue;

            var frameUrls = frameNames
                .Select(name => entryIndices.TryGetValue(name, out var index) ? entries[index].Url : null)
                .ToArray();
            if (frameUrls.Any(url => url is null))
            {
                warnings.Add($"{packageName}/{export.Name}: one or more animation frames were not published.");
                continue;
            }
            var minFrameRate = export.MinFrameRate > 0 ? export.MinFrameRate : current.MinFrameRate;
            var maxFrameRate = export.MaxFrameRate > 0 ? export.MaxFrameRate : current.MaxFrameRate;
            entries[entryIndex] = entries[entryIndex] with
            {
                Animation = new TextureAnimationManifestEntry(
                    frameUrls.Select(url => url!).ToArray(),
                    minFrameRate,
                    maxFrameRate)
            };
        }
    }

    private static void EnsureUniqueObjectNames(string packageName, IReadOnlyList<UnrealTextureExport> exports)
    {
        var duplicate = exports
            .GroupBy(export => export.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException(
                $"Package '{packageName}' contains duplicate texture object name '{duplicate.Key}' ignoring case.");
        }
    }

    private static void EnsureUniqueMeshNames(string packageName, IReadOnlyList<UnrealStaticMesh> meshes)
    {
        var duplicate = meshes
            .GroupBy(mesh => mesh.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException(
                $"Package '{packageName}' contains duplicate static mesh object name '{duplicate.Key}' ignoring case.");
        }
    }

    private static void RequireSafeSegment(string value, string description)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".." || value.Any(character =>
            char.IsControl(character) || character is '/' or '\\'))
        {
            throw new InvalidDataException($"The {description} '{value}' cannot be used as an asset path segment.");
        }
    }

    private static string HashSourceSet(IEnumerable<PackageSource> packages)
    {
        var value = string.Join(
            '\n',
            packages.Select(package => $"{package.FileName}\t{package.Sha256}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string HashSourceSet(IEnumerable<(string FileName, string Sha256)> sources)
    {
        var value = string.Join('\n', sources.Select(source => $"{source.FileName}\t{source.Sha256}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string FormatName(UnrealTextureFormat format) => format switch
    {
        UnrealTextureFormat.P8 => "p8",
        UnrealTextureFormat.Dxt1 => "dxt1",
        UnrealTextureFormat.Rgba8 => "rgba8",
        UnrealTextureFormat.Dxt3 => "dxt3",
        UnrealTextureFormat.Dxt5 => "dxt5",
        UnrealTextureFormat.G16 => "g16",
        _ => format.ToString().ToLowerInvariant()
    };

    private static string FormatName(byte? format) => format switch
    {
        0 => "p8",
        3 => "dxt1",
        5 => "rgba8",
        7 => "dxt3",
        8 => "dxt5",
        10 => "g16",
        null => "unknown",
        _ => $"format-{format}"
    };

    private static async Task<TextureConversionResult> ConvertTextureAsync(
        UnrealTextureExport export,
        CancellationToken cancellationToken)
    {
        try
        {
            if (export.MipCount == 0)
            {
                return new TextureConversionResult(
                    export,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "Texture export contains no native mip data.",
                    false);
            }

            var texture = export.Texture ?? throw new InvalidDataException(
                $"Texture '{export.Name}' has no supported pixel payload.");
            var image = await WebpTextureEncoder.EncodeLosslessAsync(texture, cancellationToken);
            var gpuImage = KtxTextureEncoder.CanEncode(texture)
                ? KtxTextureEncoder.Encode(texture)
                : null;
            var imageHash = Convert.ToHexStringLower(SHA256.HashData(image));
            var gpuImageHash = gpuImage is null
                ? null
                : Convert.ToHexStringLower(SHA256.HashData(gpuImage));
            using var versionHasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            versionHasher.AppendData(image);
            if (gpuImage is not null)
            {
                versionHasher.AppendData(gpuImage);
            }
            return new TextureConversionResult(
                export,
                image,
                imageHash,
                gpuImage,
                gpuImageHash,
                Convert.ToHexStringLower(versionHasher.GetHashAndReset()),
                null,
                false);
        }
        catch (InvalidDataException exception)
        {
            return new TextureConversionResult(
                export,
                null,
                null,
                null,
                null,
                null,
                exception.Message,
                true);
        }
    }
}

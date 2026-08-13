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
    private async Task ImportSoundsAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken)
    {
        var sourcePath = Path.GetFullPath(job.ConversionSourcePath ?? job.SourcePath);
        var assetRootPath = AssetRoot(job);
        var paths = SourceFiles(sourcePath, ".uax", "sound");
        if (paths.Length == 0)
            throw new InvalidOperationException("The configured sound directory contains no .uax packages.");

        var sourceHashes = new List<(string FileName, string Sha256)>(paths.Length);
        var packages = new List<(string Path, string PackageName, int SoundCount)>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(path);
            var packageName = Path.GetFileNameWithoutExtension(path);
            RequireSafeSegment(packageName, "sound package name");
            var encrypted = await File.ReadAllBytesAsync(path, cancellationToken);
            sourceHashes.Add((fileName, Convert.ToHexStringLower(SHA256.HashData(encrypted))));
            var sounds = new UnrealPackageReader(
                LineagePackageDecoder.DecodeProtocol111(encrypted)).ReadSoundExports();
            packages.Add((path, packageName, sounds.Count));
            job.TotalCount += sounds.Count;
        }
        job.SourceHash = sourceHashes.Single().Sha256;
        await context.SaveChangesAsync(cancellationToken);

        var (finalPath, stagingPath, sourceFolder) = OutputPaths(assetRootPath, job);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        Directory.CreateDirectory(stagingPath);
        try
        {
            var entries = new List<SoundManifestEntry>(job.TotalCount);
            foreach (var package in packages)
            {
                var packagePath = Path.Combine(stagingPath, package.PackageName);
                Directory.CreateDirectory(packagePath);
                var encrypted = await File.ReadAllBytesAsync(package.Path, cancellationToken);
                var sounds = new UnrealPackageReader(
                    LineagePackageDecoder.DecodeProtocol111(encrypted)).ReadSoundExports();
                if (sounds.Count != package.SoundCount)
                    throw new InvalidDataException($"Sound package '{package.PackageName}' changed during import.");
                foreach (var sound in sounds)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    RequireSafeSegment(sound.Name, "sound object name");
                    var fileName = $"{sound.Name}.wav";
                    var hash = Convert.ToHexStringLower(SHA256.HashData(sound.WaveData));
                    await File.WriteAllBytesAsync(
                        Path.Combine(packagePath, fileName),
                        sound.WaveData,
                        cancellationToken);
                    entries.Add(new SoundManifestEntry(
                        package.PackageName,
                        sound.Name,
                        VersionedUrl(sourceFolder, package.PackageName, fileName, hash),
                        sound.DurationSeconds,
                        sound.SampleRate,
                        sound.Channels,
                        sound.WaveData.LongLength,
                        hash,
                        job.SourceKey));
                    job.ProcessedCount++;
                    await SaveProgressAsync(context, job, cancellationToken);
                }
            }
            await File.WriteAllTextAsync(Path.Combine(stagingPath, ".l2-asset-version"), job.SourceHash, cancellationToken);
            Promote(stagingPath, finalPath);
            await PublishCatalogAsync(context, job, finalPath, sourceFolder, 2, 111, Array.Empty<string>(), entries,
                group => group, item => item.ObjectName, item => item.PackageName, _ => "resolved", new { }, cancellationToken);
            job.Status = AssetImportJobValues.Succeeded;
            job.FinishedAt = timeProvider.GetUtcNow();
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (Directory.Exists(stagingPath)) Directory.Delete(stagingPath, recursive: true);
        }
    }

    private async Task ImportMusicAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken)
    {
        var sourcePath = Path.GetFullPath(job.ConversionSourcePath ?? job.SourcePath);
        var assetRootPath = AssetRoot(job);
        var paths = SourceFiles(sourcePath, ".ogg", "music");
        if (paths.Length == 0)
        {
            throw new InvalidOperationException("The configured music directory contains no .ogg files.");
        }

        var sources = new List<MusicSource>(paths.Length);
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(path);
            RequireSafeSegment(fileName, "music file name");
            var sourceBytes = await File.ReadAllBytesAsync(path, cancellationToken);
            sources.Add(new MusicSource(
                path,
                fileName,
                Convert.ToHexStringLower(SHA256.HashData(sourceBytes))));
        }

        var duplicateFile = sources
            .GroupBy(source => source.FileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFile is not null)
        {
            throw new InvalidDataException(
                $"Music file name '{duplicateFile.Key}' is duplicated ignoring case.");
        }

        job.TotalCount = sources.Count;
        job.SourceHash = sources.Single().Sha256;
        await context.SaveChangesAsync(cancellationToken);

        Directory.CreateDirectory(assetRootPath);
        var (finalPath, stagingPath, sourceFolder) = OutputPaths(assetRootPath, job);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        Directory.CreateDirectory(stagingPath);
        try
        {
            var entries = new List<MusicManifestEntry>(sources.Count);
            var warnings = new List<string>();
            foreach (var source in sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var input = await File.ReadAllBytesAsync(source.Path, cancellationToken);
                    var track = L2MusicDecoder.Decode(input);
                    var hash = Convert.ToHexStringLower(SHA256.HashData(track.Data));
                    await File.WriteAllBytesAsync(
                        Path.Combine(stagingPath, source.FileName),
                        track.Data,
                        cancellationToken);
                    entries.Add(new MusicManifestEntry(
                        Path.GetFileNameWithoutExtension(source.FileName),
                        source.FileName,
                        VersionedFileUrl(sourceFolder, source.FileName, hash),
                        track.DurationSeconds,
                        track.SampleRate,
                        track.Channels,
                        track.Data.LongLength,
                        hash,
                        "resolved",
                        null,
                        job.SourceKey));
                }
                catch (InvalidDataException exception)
                {
                    warnings.Add($"{source.FileName}: {exception.Message}");
                    entries.Add(new MusicManifestEntry(
                        Path.GetFileNameWithoutExtension(source.FileName),
                        source.FileName,
                        null,
                        null,
                        null,
                        null,
                        new FileInfo(source.Path).Length,
                        null,
                        "skipped",
                        exception.Message,
                        job.SourceKey));
                    job.SkippedCount++;
                }

                job.ProcessedCount++;
                await SaveProgressAsync(context, job, cancellationToken);
            }

            job.WarningsJson = JsonSerializer.Serialize(warnings);
            await File.WriteAllTextAsync(Path.Combine(stagingPath, ".l2-asset-version"), job.SourceHash, cancellationToken);
            Promote(stagingPath, finalPath);
            await PublishCatalogAsync(context, job, finalPath, sourceFolder, 2, null, Array.Empty<string>(), entries,
                group => group, item => item.Name, _ => null, item => item.Status, new { }, cancellationToken);
            job.Status = warnings.Count == 0
                ? AssetImportJobValues.Succeeded
                : AssetImportJobValues.SucceededWithWarnings;
            job.FinishedAt = timeProvider.GetUtcNow();
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Imported {ProcessedCount} music tracks with {SkippedCount} skipped for job {JobId}",
                job.ProcessedCount,
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

}

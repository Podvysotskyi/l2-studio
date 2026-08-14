using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;
using Microsoft.Extensions.Logging;

namespace L2.Studio.Services;

public sealed partial class AssetImportJobProcessor
{
    private async Task ImportAnimationsAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken)
    {
        AssetImportSourcePaths.RequireSupportedVersion(job.Kind, job.GameVersion);
        var sourcePath = Path.GetFullPath(job.ConversionSourcePath ?? job.SourcePath);
        var packagePaths = SourceFiles(sourcePath, ".ukx", "animation");
        if (packagePaths.Length == 0)
            throw new InvalidOperationException("The configured animation directory contains no .ukx packages.");
        var packagePath = packagePaths.Single();
        var fileName = Path.GetFileName(packagePath);
        var packageName = Path.GetFileNameWithoutExtension(fileName);
        RequireSafeSegment(packageName, "package name");
        var encrypted = await File.ReadAllBytesAsync(packagePath, cancellationToken);
        var fileHash = Convert.ToHexStringLower(SHA256.HashData(encrypted));
        var package = new UnrealPackageReader(LineagePackageDecoder.DecodeProtocol111(encrypted)).ReadAnimationPackage();
        job.SourceHash = fileHash;
        job.TotalCount = package.SkeletalMeshes.Count;
        await context.SaveChangesAsync(cancellationToken);

        var assetRootPath = AssetRoot(job);
        Directory.CreateDirectory(assetRootPath);
        var (finalPath, stagingPath, sourceFolder) = OutputPaths(assetRootPath, job);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        Directory.CreateDirectory(stagingPath);
        try
        {
            var warnings = new List<string>();
            if (package.UnsupportedVertexMeshCount > 0)
                warnings.Add($"{fileName}: skipped {package.UnsupportedVertexMeshCount} unsupported VertMesh exports.");
            var animationByPath = package.AnimationSets.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
            var animationByName = package.AnimationSets.GroupBy(item => LeafName(item.Name), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);
            var animationAssets = new Dictionary<string, (AnimationSetManifestEntry Manifest, string Signature)>(StringComparer.OrdinalIgnoreCase);
            foreach (var animation in package.AnimationSets.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                var objectName = LeafName(animation.Name);
                RequireSafeSegment(objectName, "animation set object name");
                var glb = GlbAnimationEncoder.Encode(animation);
                var hash = Convert.ToHexStringLower(SHA256.HashData(glb));
                var outputName = $"{objectName}.animations.glb";
                await File.WriteAllBytesAsync(Path.Combine(stagingPath, outputName), glb, cancellationToken);
                var signature = SkeletonSignature(animation.Bones.Select(item => (item.Name, item.ParentIndex)));
                animationAssets[animation.Name] = (
                    AnimationSetManifest(animation, VersionedFileUrl(sourceFolder, outputName, hash), hash), signature);
            }
            var animationSets = animationAssets.Values.Select(item => item.Manifest).ToArray();
            var entries = new List<AnimationMeshManifestEntry>(package.SkeletalMeshes.Count);
            foreach (var mesh in package.SkeletalMeshes.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var objectName = LeafName(mesh.Name);
                RequireSafeSegment(objectName, "skeletal mesh object name");
                var animation = ResolveAnimation(mesh.Animation, animationByPath, animationByName);
                var skeletonSignature = SkeletonSignature(mesh.Bones.Select(item => (item.Name, item.ParentIndex)));
                var compatible = animation is not null && skeletonSignature ==
                    SkeletonSignature(animation.Bones.Select(item => (item.Name, item.ParentIndex)));
                try
                {
                    if (mesh.Error is not null) throw new InvalidDataException(mesh.Error);
                    var glb = GlbSkeletalMeshEncoder.Encode(mesh);
                    var hash = Convert.ToHexStringLower(SHA256.HashData(glb));
                    var outputName = $"{objectName}.glb";
                    await File.WriteAllBytesAsync(Path.Combine(stagingPath, outputName), glb, cancellationToken);
                    if (animation is not null && !compatible)
                        warnings.Add($"{fileName}/{objectName}: linked animation set '{animation.Name}' has an incompatible skeleton.");
                    entries.Add(new AnimationMeshManifestEntry(
                        packageName, objectName, VersionedFileUrl(sourceFolder, outputName, hash),
                        mesh.Positions.Count, mesh.Indices.Count / 3, mesh.Sections.Count, mesh.Bones.Count,
                        skeletonSignature, compatible ? LeafName(animation!.Name) : null,
                        compatible ? animationAssets[animation!.Name].Manifest.Url : null,
                        compatible ? animation!.Clips.Select(AnimationClipManifest).ToArray() : [],
                        mesh.Sections.Count(item => item.Material is not null),
                        mesh.Sections.Any(item => item.Material is not null) ? "referenced" : "runtime",
                        hash, "resolved", null, job.SourceKey));
                }
                catch (InvalidDataException exception)
                {
                    warnings.Add($"{fileName}/{objectName}: {exception.Message}");
                    entries.Add(new AnimationMeshManifestEntry(
                        packageName, objectName, null, mesh.Positions.Count, mesh.Indices.Count / 3,
                        mesh.Sections.Count, mesh.Bones.Count, skeletonSignature, null, null, [],
                        mesh.Sections.Count(item => item.Material is not null), "unavailable", null,
                        "skipped", exception.Message, job.SourceKey));
                    job.SkippedCount++;
                }
                job.ProcessedCount++;
                await SaveProgressAsync(context, job, cancellationToken);
            }

            var packageEntry = new AnimationManifestPackage(
                packageName, fileName, fileHash, package.SkeletalMeshes.Count, package.AnimationSets.Count,
                package.AnimationSets.Sum(item => item.Clips.Count),
                package.AnimationSets.Sum(item => item.Clips.Sum(clip => clip.Notifies.Count)),
                package.UnsupportedVertexMeshCount, job.SourceKey);
            var manifest = new AnimationManifest(
                1, AssetImportJobValues.Animations, sourceFolder, fileHash, 111,
                [packageEntry], entries, animationSets);
            await File.WriteAllTextAsync(
                Path.Combine(stagingPath, "manifest.json"),
                JsonSerializer.Serialize(manifest, ManifestJsonOptions), cancellationToken);
            job.WarningsJson = JsonSerializer.Serialize(warnings);
            await File.WriteAllTextAsync(Path.Combine(stagingPath, ".l2-asset-version"), fileHash, cancellationToken);
            Promote(stagingPath, finalPath);
            await PublishCatalogAsync(
                context, job, finalPath, sourceFolder, 1, 111, [packageEntry], entries,
                group => group.Name, item => item.ObjectName, item => item.PackageName, item => item.Status,
                new { animationSets, unsupportedVertexMeshes = package.UnsupportedVertexMeshCount }, cancellationToken);
            job.Status = warnings.Count == 0 ? AssetImportJobValues.Succeeded : AssetImportJobValues.SucceededWithWarnings;
            job.FinishedAt = timeProvider.GetUtcNow();
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Imported {ProcessedCount} skeletal meshes and {AnimationSetCount} animation sets for job {JobId}",
                job.ProcessedCount, package.AnimationSets.Count, job.Id);
        }
        finally
        {
            if (Directory.Exists(stagingPath)) Directory.Delete(stagingPath, recursive: true);
        }
    }

    private static UnrealMeshAnimation? ResolveAnimation(
        UnrealObjectReference? reference,
        IReadOnlyDictionary<string, UnrealMeshAnimation> byPath,
        IReadOnlyDictionary<string, UnrealMeshAnimation> byName)
    {
        if (reference is null) return null;
        return byPath.GetValueOrDefault(reference.Path) ?? byPath.GetValueOrDefault(reference.ObjectName) ??
            byName.GetValueOrDefault(LeafName(reference.ObjectName));
    }

    private static AnimationSetManifestEntry AnimationSetManifest(UnrealMeshAnimation animation, string url, string hash) => new(
        LeafName(animation.Name), url, hash, animation.Bones.Count,
        SkeletonSignature(animation.Bones.Select(item => (item.Name, item.ParentIndex))),
        animation.Clips.Select(AnimationClipManifest).ToArray());

    private static AnimationClipManifestEntry AnimationClipManifest(UnrealAnimationClip clip) => new(
        clip.Name, clip.FrameCount, clip.FrameRate, clip.DurationSeconds, clip.Groups,
        clip.Notifies.Select(notify => new AnimationNotifyManifestEntry(
            notify.NormalizedTime,
            notify.NormalizedTime * clip.DurationSeconds,
            notify.FunctionName,
            notify.ObjectPath,
            notify.ClassName,
            notify.Properties)).ToArray());

    private static string SkeletonSignature(IEnumerable<(string Name, int Parent)> bones)
    {
        var value = string.Join('\n', bones.Select(item => $"{item.Name.ToLowerInvariant()}\0{item.Parent}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string LeafName(string path)
    {
        var separator = path.LastIndexOf('.');
        return separator < 0 ? path : path[(separator + 1)..];
    }
}

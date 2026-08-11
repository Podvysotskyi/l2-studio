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
    private async Task ImportStaticMeshesAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken)
    {
        var sourcePath = Path.GetFullPath(job.ConversionSourcePath ?? job.SourcePath);
        var assetRootPath = Path.GetFullPath(options.Value.AssetRootPath);
        var packagePaths = SourceFiles(sourcePath, ".usx", "static-mesh");
        if (packagePaths.Length == 0)
        {
            throw new InvalidOperationException("The configured static-mesh directory contains no .usx packages.");
        }

        var packages = new List<StaticMeshPackageSource>(packagePaths.Length);
        var materialReferences = new List<TextureMaterialReference>();
        var embeddedMaterials = new List<TextureMaterialManifestEntry>();
        foreach (var packagePath in packagePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(packagePath);
            var packageName = Path.GetFileNameWithoutExtension(fileName);
            RequireSafeSegment(packageName, "package name");
            var encrypted = await File.ReadAllBytesAsync(packagePath, cancellationToken);
            var fileHash = Convert.ToHexStringLower(SHA256.HashData(encrypted));
            var reader = new UnrealPackageReader(LineagePackageDecoder.DecodeProtocol111(encrypted));
            var meshes = reader.ReadStaticMeshes();
            embeddedMaterials.AddRange(reader.ReadMaterialExports().Select(material =>
                MaterialManifest(packageName, material)));
            EnsureUniqueMeshNames(packageName, meshes);
            foreach (var mesh in meshes)
            {
                RequireSafeSegment(mesh.Name, "static mesh object name");
                materialReferences.AddRange(mesh.Sections
                    .Select(section => MaterialReference(packageName, section.Material))
                    .OfType<TextureMaterialReference>());
            }
            packages.Add(new StaticMeshPackageSource(packagePath, packageName, fileName, fileHash, meshes.Count));
            job.TotalCount += meshes.Count;
        }

        job.SourceHash = packages.Single().Sha256;
        await context.SaveChangesAsync(cancellationToken);
        var materialCatalog = await StaticMeshMaterialCatalogLoader.LoadAsync(
            context,
            materialReferences,
            embeddedMaterials,
            cancellationToken);
        var materialResolver = materialCatalog.Resolver;

        Directory.CreateDirectory(assetRootPath);
        var (finalPath, stagingPath, sourceFolder) = OutputPaths(assetRootPath, job);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        Directory.CreateDirectory(stagingPath);
        try
        {
            var entries = new List<StaticMeshManifestEntry>(job.TotalCount);
            var warnings = new List<string>();
            foreach (var package in packages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var encrypted = await File.ReadAllBytesAsync(package.Path, cancellationToken);
                var meshes = new UnrealPackageReader(LineagePackageDecoder.DecodeProtocol111(encrypted))
                    .ReadStaticMeshes()
                    .OrderBy(mesh => mesh.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var packageOutputPath = Path.Combine(stagingPath, package.Name);
                Directory.CreateDirectory(packageOutputPath);
                foreach (var mesh in meshes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var material = materialResolver.Resolve(mesh, package.Name);
                    try
                    {
                        var glb = GlbStaticMeshEncoder.Encode(mesh, material.SectionMaterials);
                        var hash = Convert.ToHexStringLower(SHA256.HashData(glb));
                        var fileName = $"{mesh.Name}.glb";
                        await File.WriteAllBytesAsync(Path.Combine(packageOutputPath, fileName), glb, cancellationToken);
                        entries.Add(new StaticMeshManifestEntry(
                            package.Name,
                            mesh.Name,
                            VersionedUrl(sourceFolder, package.Name, fileName, hash),
                            mesh.Positions.Count,
                            mesh.Indices.Count / 3,
                            mesh.Sections.Count,
                            material.MaterialCount,
                            material.ResolvedMaterialCount,
                            material.Status,
                            material.Error,
                            hash,
                            "resolved",
                            null));
                    }
                    catch (InvalidDataException exception)
                    {
                        warnings.Add($"{package.FileName}/{mesh.Name}: {exception.Message}");
                        entries.Add(new StaticMeshManifestEntry(
                            package.Name,
                            mesh.Name,
                            null,
                            mesh.Positions.Count,
                            mesh.Indices.Count / 3,
                            mesh.Sections.Count,
                            material.MaterialCount,
                            material.ResolvedMaterialCount,
                            material.Status,
                            material.Error,
                            null,
                            "skipped",
                            exception.Message));
                        job.SkippedCount++;
                    }

                    job.ProcessedCount++;
                    await SaveProgressAsync(context, job, cancellationToken);
                }
            }

            var catalogGroups = packages.Select(package => new StaticMeshManifestPackage(
                    package.Name,
                    package.FileName,
                    package.Sha256,
                    package.MeshCount)).ToArray();

            job.WarningsJson = JsonSerializer.Serialize(warnings);
            await File.WriteAllTextAsync(Path.Combine(stagingPath, ".l2-asset-version"), job.SourceHash, cancellationToken);
            Promote(stagingPath, finalPath);
            await PublishCatalogAsync(context, job, finalPath, sourceFolder, 8, 111, catalogGroups, entries,
                group => group.Name, item => item.ObjectName, item => item.PackageName, item => item.Status,
                new StaticMeshCatalogMetadata(materialCatalog.GpuTextureFormats), cancellationToken);
            job.Status = warnings.Count == 0
                ? AssetImportJobValues.Succeeded
                : AssetImportJobValues.SucceededWithWarnings;
            job.FinishedAt = timeProvider.GetUtcNow();
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Imported {ProcessedCount} static meshes from {PackageCount} packages with {SkippedCount} skipped for job {JobId}",
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

}

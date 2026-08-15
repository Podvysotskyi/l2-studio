using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Tools.ClientData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace L2.Studio.Services;

public sealed partial class AssetImportJobProcessor
{
    private static readonly JsonSerializerOptions NpcAppearanceManifestJsonOptions = CreateNpcAppearanceManifestJsonOptions();

    private async Task ImportNpcAppearancesAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken)
    {
        AssetImportSourcePaths.RequireSupportedVersion(job.Kind, job.GameVersion);
        var sourcePath = Path.GetFullPath(job.ConversionSourcePath ?? job.SourcePath);
        if (!AssetImportSourcePaths.MatchesKind(job.Kind, sourcePath))
            throw new InvalidDataException("The NPC appearance source must be system/npcgrp.txt.");

        var source = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
        var records = NpcGrpReader.ReadProtocol211(source);
        var animationSchemaVersion = await context.AssetCatalogs.AsNoTracking()
            .Where(catalog => catalog.GameVersion == job.GameVersion &&
                catalog.Kind == AssetImportJobValues.Animations && catalog.IsActive)
            .Select(catalog => (int?)catalog.SchemaVersion)
            .SingleOrDefaultAsync(cancellationToken);
        if (animationSchemaVersion is null or < 2)
            throw new InvalidDataException("Import the schema-v2 C1 animation catalog before importing NPC appearances.");
        var candidates = await LoadNpcAssetCandidatesAsync(context, job.GameVersion, cancellationToken);
        var textureReferenceResolver = await LoadNpcTextureReferenceResolverAsync(
            context, job.GameVersion, cancellationToken);
        var textureMatches = new Dictionary<string, (TextureMaterialReference? Reference, int MatchCount)>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var reference in records.SelectMany(record => record.Textures)
                     .Where(reference => !string.IsNullOrWhiteSpace(reference))
                     .Select(reference => reference.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var match = textureReferenceResolver.Resolve(reference, out var matchCount);
            textureMatches[reference] = (match, matchCount);
        }
        var materialReferences = textureMatches.Values
            .Select(match => match.Reference)
            .OfType<TextureMaterialReference>()
            .Concat(candidates.Where(candidate => candidate.Kind == AssetImportJobValues.Animations)
                .SelectMany(candidate => candidate.DefaultMaterials)
                .Select(slot => slot.Reference)
                .OfType<TextureMaterialReference>())
            .GroupBy(reference => $"{reference.PackageName}\n{reference.ObjectName}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        var materialCatalog = await StaticMeshMaterialCatalogLoader.LoadAsync(
            context, job.GameVersion, materialReferences, cancellationToken);
        await TrackTextureDependenciesAsync(context, job.GameVersion, materialReferences, cancellationToken);
        var warnings = new HashSet<string>(materialCatalog.Warnings, StringComparer.Ordinal);
        var resolvedCount = 0;
        var unresolvedCount = 0;

        NpcAssetReference Resolve(string reference, string kind, string description)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                unresolvedCount++;
                return new NpcAssetReference(reference, null);
            }

            var key = reference.Trim();
            var matches = candidates.Where(candidate => candidate.Kind == kind &&
                (string.Equals(candidate.QualifiedName, key, StringComparison.OrdinalIgnoreCase) ||
                 (!key.Contains('.') && string.Equals(candidate.ObjectName, key, StringComparison.OrdinalIgnoreCase))))
                .ToArray();
            var normalizedKey = key.ToLowerInvariant();
            if (matches.Length == 1 && !string.IsNullOrWhiteSpace(matches[0].Url))
            {
                var match = matches[0];
                resolvedCount++;
                dependencyHints.Add(new AssetCatalogDependencyPublication(
                    kind, normalizedKey, match.NormalizedSourceKey,
                    match.ArtifactFingerprint ?? match.SourceHash, true, match.OutputRoot));
                return new NpcAssetReference(key, match.Url);
            }

            unresolvedCount++;
            dependencyHints.Add(new AssetCatalogDependencyPublication(kind, normalizedKey, null, null, false, null));
            warnings.Add(matches.Length == 0
                ? $"{description} '{key}' is not published."
                : $"{description} '{key}' is ambiguous across {matches.Length} published assets.");
            return new NpcAssetReference(key, null);
        }

        (NpcAnimationAssetReference Asset, NpcAssetCandidate? Candidate) ResolveMesh(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                unresolvedCount++;
                return (new NpcAnimationAssetReference(reference, null, null), null);
            }

            var key = reference.Trim();
            var matches = candidates.Where(candidate => candidate.Kind == AssetImportJobValues.Animations &&
                (string.Equals(candidate.QualifiedName, key, StringComparison.OrdinalIgnoreCase) ||
                 (!key.Contains('.') && string.Equals(candidate.ObjectName, key, StringComparison.OrdinalIgnoreCase))))
                .ToArray();
            var normalizedKey = key.ToLowerInvariant();
            if (matches.Length == 1 && !string.IsNullOrWhiteSpace(matches[0].Url))
            {
                var match = matches[0];
                resolvedCount++;
                dependencyHints.Add(new AssetCatalogDependencyPublication(
                    AssetImportJobValues.Animations, normalizedKey, match.NormalizedSourceKey,
                    match.ArtifactFingerprint ?? match.SourceHash, true, match.OutputRoot));
                return (new NpcAnimationAssetReference(key, match.Url, match.AnimationUrl), match);
            }

            unresolvedCount++;
            dependencyHints.Add(new AssetCatalogDependencyPublication(
                AssetImportJobValues.Animations, normalizedKey, null, null, false, null));
            warnings.Add(matches.Length == 0
                ? $"Animation mesh '{key}' is not published."
                : $"Animation mesh '{key}' is ambiguous across {matches.Length} published assets.");
            return (new NpcAnimationAssetReference(key, null, null), null);
        }

        NpcMaterialReference ResolveTexture(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                unresolvedCount++;
                return new NpcMaterialReference(reference, null, null);
            }

            var key = reference.Trim();
            var normalizedKey = key.ToLowerInvariant();
            var match = textureMatches[key];
            if (match.Reference is null)
            {
                unresolvedCount++;
                dependencyHints.Add(new AssetCatalogDependencyPublication(
                    AssetImportJobValues.Textures, normalizedKey, null, null, false, null));
                warnings.Add(match.MatchCount == 0
                    ? $"Texture or material '{key}' is not published."
                    : $"Texture or material '{key}' is ambiguous across {match.MatchCount} published assets.");
                return new NpcMaterialReference(key, null, null);
            }

            try
            {
                var material = materialCatalog.Resolver.Resolve(match.Reference);
                resolvedCount++;
                return new NpcMaterialReference(key, material.DiffuseUrl, material);
            }
            catch (InvalidDataException exception)
            {
                unresolvedCount++;
                warnings.Add($"Texture or material '{key}' could not be resolved: {exception.Message}");
                return new NpcMaterialReference(key, null, null);
            }
        }

        var defaultMaterialCache = new Dictionary<string, NpcMaterialReference>(StringComparer.OrdinalIgnoreCase);
        NpcMaterialReference ResolveDefaultMaterial(TextureMaterialReference reference)
        {
            var key = $"{reference.PackageName}.{reference.ObjectName}";
            if (defaultMaterialCache.TryGetValue(key, out var cached)) return cached;
            try
            {
                var material = materialCatalog.Resolver.Resolve(reference);
                resolvedCount++;
                var resolved = new NpcMaterialReference(key, material.DiffuseUrl, material);
                defaultMaterialCache[key] = resolved;
                return resolved;
            }
            catch (InvalidDataException exception)
            {
                unresolvedCount++;
                warnings.Add($"Default skeletal material '{key}' could not be resolved: {exception.Message}");
                var unresolved = new NpcMaterialReference(key, null, null);
                defaultMaterialCache[key] = unresolved;
                return unresolved;
            }
        }

        NpcAppearanceMaterialSlot[] ComposeMaterialSlots(
            NpcAssetCandidate? mesh,
            IReadOnlyList<NpcMaterialReference> overrides)
        {
            if (mesh is null) return [];
            if (overrides.Count > mesh.DefaultMaterials.Count)
                warnings.Add($"Mesh '{mesh.QualifiedName}' has {overrides.Count} NPC texture overrides for {mesh.DefaultMaterials.Count} sections.");
            var defaults = mesh.DefaultMaterials.Select(slot =>
                slot.Reference is null ? null : ResolveDefaultMaterial(slot.Reference)).ToArray();
            return ComposeNpcMaterialSlots(defaults, overrides);
        }

        NpcAssetReference Effect(string reference)
        {
            if (!string.IsNullOrWhiteSpace(reference)) unresolvedCount++;
            return new NpcAssetReference(reference, null);
        }

        var appearanceEntries = records.Select(record =>
        {
            var mesh = ResolveMesh(record.Mesh);
            var textures = record.Textures.Select(ResolveTexture).ToArray();
            return new NpcAppearanceManifestEntry(
                checked((int)record.Id),
                record.Id,
                record.Name,
                record.Speed,
                record.ClassName,
                mesh.Asset,
                textures,
                ComposeMaterialSlots(mesh.Candidate, textures),
                record.CollisionRadius,
                record.CollisionHeight,
                record.AttackSounds.Select(reference => Resolve(reference, AssetImportJobValues.Sounds, "Sound")).ToArray(),
                record.DefenceSounds.Select(reference => Resolve(reference, AssetImportJobValues.Sounds, "Sound")).ToArray(),
                record.DamageSounds.Select(reference => Resolve(reference, AssetImportJobValues.Sounds, "Sound")).ToArray(),
                record.SoundVolume,
                record.SoundRadius,
                record.SoundRandomness,
                Effect(record.AttackEffect));
        }).ToArray();
        if (appearanceEntries.Select(entry => entry.AppearanceId).Distinct().Count() != appearanceEntries.Length)
            throw new InvalidDataException("The NPC appearance source contains duplicate appearance identifiers.");

        var npcMappings = await context.Npcs.AsNoTracking()
            .Where(npc => npc.GameVersion == job.GameVersion)
            .OrderBy(npc => npc.Id)
            .Select(npc => new { npc.Id, npc.AppearanceId })
            .ToArrayAsync(cancellationToken);
        if (npcMappings.Length == 0)
            throw new InvalidDataException("Import the C1 NPC definitions before importing NPC appearances.");

        var entries = MatchNpcAppearances(
            appearanceEntries,
            npcMappings.Select(mapping => (mapping.Id, mapping.AppearanceId)).ToArray());
        if (entries.Length == 0)
            throw new InvalidDataException("None of the imported NPC definitions match the C1 npcgrp appearances.");

        var unmatchedNpcCount = npcMappings.Length - entries.Length;
        var usedAppearanceIds = entries.Select(entry => entry.AppearanceId).ToHashSet();
        var unusedAppearanceCount = appearanceEntries.Count(entry => !usedAppearanceIds.Contains(entry.AppearanceId));
        if (unmatchedNpcCount > 0)
            warnings.Add($"{unmatchedNpcCount} imported NPC definitions have no appearance in the C1 npcgrp source.");

        job.TotalResourceCount = appearanceEntries.Length;
        job.ProcessedResourceCount = appearanceEntries.Length;
        job.WarningsJson = JsonSerializer.Serialize(warnings.Order(StringComparer.Ordinal).ToArray());
        var (finalPath, stagingPath, sourceFolder) = OutputPaths(options.Value.AssetRootPath, job);
        ResetDirectory(stagingPath);
        try
        {
            foreach (var entry in entries)
            {
                var manifest = new NpcAppearanceManifest(
                    6, AssetImportJobValues.NpcAppearances, job.SourceKey, job.SourceHash!, 211, entry);
                var manifestPath = Path.Combine(stagingPath, NpcAppearanceManifestRelativePath(entry.Id));
                Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
                await File.WriteAllTextAsync(
                    manifestPath,
                    SerializeNpcAppearanceManifest(manifest), cancellationToken);
            }
            await File.WriteAllTextAsync(Path.Combine(stagingPath, ".l2-asset-version"), job.SourceHash!, cancellationToken);
            Promote(stagingPath, finalPath);
            await PublishCatalogAsync(
                context, job, finalPath, sourceFolder, 6, 211,
                Array.Empty<string>(), Array.Empty<string>(),
                value => value, value => value, _ => null, _ => "resolved",
                new NpcAppearanceCatalogMetadata(
                    $"/{EscapedUrlRoot(sourceFolder)}/npcs/{{id}}/manifest.json",
                    entries.Select(entry => entry.Id).Order().ToArray(),
                    entries.Length,
                    appearanceEntries.Length,
                    entries.Length,
                    unmatchedNpcCount,
                    unusedAppearanceCount,
                    resolvedCount,
                    unresolvedCount),
                cancellationToken);
            job.Status = warnings.Count == 0
                ? AssetImportJobValues.Succeeded
                : AssetImportJobValues.SucceededWithWarnings;
            job.FinishedAt = timeProvider.GetUtcNow();
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Published {NpcCount} NPC manifests from {AppearanceCount} client appearances with {ResolvedReferenceCount} resolved references for job {JobId}",
                entries.Length, appearanceEntries.Length, resolvedCount, job.Id);
        }
        finally
        {
            if (Directory.Exists(stagingPath)) Directory.Delete(stagingPath, recursive: true);
        }
    }

    private static async Task<NpcAssetCandidate[]> LoadNpcAssetCandidatesAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        var kinds = new[]
        {
            AssetImportJobValues.Animations,
            AssetImportJobValues.Sounds
        };
        var rows = await context.AssetCatalogItems.AsNoTracking()
            .Where(item => item.Catalog.GameVersion == gameVersion && item.Catalog.IsActive && kinds.Contains(item.Catalog.Kind))
            .Select(item => new
            {
                Kind = item.Catalog.Kind,
                item.Name,
                item.GroupName,
                item.MetadataJson,
                item.Source.NormalizedSourceKey,
                item.Source.ArtifactFingerprint,
                item.Source.SourceHash,
                item.Source.OutputRoot
            }).ToArrayAsync(cancellationToken);

        return rows.Select(row =>
        {
            using var json = JsonDocument.Parse(row.MetadataJson);
            var root = json.RootElement;
            var packageName = root.TryGetProperty("packageName", out var package) ? package.GetString() : row.GroupName;
            var objectName = root.TryGetProperty("objectName", out var name) ? name.GetString() : row.Name;
            var url = root.TryGetProperty("url", out var urlProperty) && urlProperty.ValueKind == JsonValueKind.String
                ? urlProperty.GetString()
                : null;
            var animationUrl = root.TryGetProperty("animationUrl", out var animationUrlProperty) &&
                animationUrlProperty.ValueKind == JsonValueKind.String
                ? animationUrlProperty.GetString()
                : null;
            var defaultMaterials = row.Kind == AssetImportJobValues.Animations &&
                root.TryGetProperty("defaultMaterials", out var defaultMaterialsProperty)
                ? JsonSerializer.Deserialize<AnimationMeshMaterialSlot[]>(
                    defaultMaterialsProperty.GetRawText(), ManifestJsonOptions) ?? []
                : [];
            objectName ??= row.Name;
            return new NpcAssetCandidate(
                row.Kind,
                objectName,
                string.IsNullOrWhiteSpace(packageName) ? objectName : $"{packageName}.{objectName}",
                url,
                animationUrl,
                row.NormalizedSourceKey,
                row.ArtifactFingerprint,
                row.SourceHash,
                row.OutputRoot,
                defaultMaterials);
        }).ToArray();
    }

    private static async Task<NpcTextureReferenceResolver> LoadNpcTextureReferenceResolverAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        var catalog = await context.AssetCatalogs.AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion &&
                item.Kind == AssetImportJobValues.Textures && item.IsActive, cancellationToken);
        if (catalog is null) return new NpcTextureReferenceResolver([]);

        var textures = catalog.Items
            .Select(item => JsonSerializer.Deserialize<TextureManifestEntry>(item.MetadataJson, ManifestJsonOptions)!)
            .Select(texture => new TextureMaterialReference(texture.PackageName, texture.ObjectName, "Texture"));
        var materials = JsonSerializer.Deserialize<TextureCatalogMetadata>(catalog.MetadataJson, ManifestJsonOptions)
            ?.Materials.Select(material => new TextureMaterialReference(
                material.PackageName, material.ObjectName, material.ClassName)) ?? [];
        return new NpcTextureReferenceResolver(textures.Concat(materials));
    }

    internal static string SerializeNpcAppearanceManifest(NpcAppearanceManifest manifest) =>
        JsonSerializer.Serialize(manifest, NpcAppearanceManifestJsonOptions);

    internal static NpcAppearanceManifestEntry[] MatchNpcAppearances(
        IReadOnlyList<NpcAppearanceManifestEntry> appearances,
        IReadOnlyList<(int NpcId, int? AppearanceId)> npcMappings)
    {
        var appearancesById = appearances.ToDictionary(entry => entry.AppearanceId);
        return npcMappings.Select(mapping =>
                mapping.AppearanceId is > 0 &&
                appearancesById.TryGetValue(checked((uint)mapping.AppearanceId.Value), out var appearance)
                    ? appearance with { Id = mapping.NpcId }
                    : null)
            .OfType<NpcAppearanceManifestEntry>()
            .ToArray();
    }

    internal static NpcAppearanceMaterialSlot[] ComposeNpcMaterialSlots(
        IReadOnlyList<NpcMaterialReference?> defaults,
        IReadOnlyList<NpcMaterialReference> overrides) =>
        defaults.Select((defaultMaterial, sectionIndex) =>
        {
            var overrideMaterial = sectionIndex < overrides.Count ? overrides[sectionIndex] : null;
            var effectiveMaterial = overrideMaterial?.Material is not null
                ? overrideMaterial
                : defaultMaterial?.Material is not null ? defaultMaterial : null;
            var effectiveSource = overrideMaterial?.Material is not null
                ? "override"
                : defaultMaterial?.Material is not null ? "default" : "fallback";
            var warning = overrideMaterial is not null && overrideMaterial.Material is null
                ? effectiveMaterial is null
                    ? $"Override '{overrideMaterial.Reference}' and the mesh default are unavailable."
                    : $"Override '{overrideMaterial.Reference}' is unavailable; using the mesh default."
                : effectiveMaterial is null && defaultMaterial is not null
                    ? $"Default material '{defaultMaterial.Reference}' is unavailable."
                    : null;
            return new NpcAppearanceMaterialSlot(
                sectionIndex,
                defaultMaterial,
                overrideMaterial,
                effectiveMaterial,
                effectiveSource,
                warning);
        }).ToArray();

    internal static string NpcAppearanceManifestRelativePath(int npcId) =>
        $"npcs/{npcId.ToString(CultureInfo.InvariantCulture)}/manifest.json";

    private static JsonSerializerOptions CreateNpcAppearanceManifestJsonOptions()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        serializerOptions.Converters.Add(new JsonStringEnumConverter(new LowerCaseJsonNamingPolicy()));
        return serializerOptions;
    }

    private sealed class LowerCaseJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name.ToLowerInvariant();
    }

    private sealed record NpcAssetCandidate(
        string Kind,
        string ObjectName,
        string QualifiedName,
        string? Url,
        string? AnimationUrl,
        string NormalizedSourceKey,
        string? ArtifactFingerprint,
        string SourceHash,
        string OutputRoot,
        IReadOnlyList<AnimationMeshMaterialSlot> DefaultMaterials);
}

using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace L2.Studio.Services;

internal static class StaticMeshMaterialCatalogLoader
{
    private const int LookupBatchSize = 1_000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<StaticMeshMaterialCatalog> LoadAsync(
        GameContentDbContext context,
        string gameVersion,
        IReadOnlyCollection<TextureMaterialReference> rootReferences,
        CancellationToken cancellationToken) => await LoadAsync(
            context,
            gameVersion,
            rootReferences,
            [],
            cancellationToken);

    public static async Task<StaticMeshMaterialCatalog> LoadAsync(
        GameContentDbContext context,
        string gameVersion,
        IReadOnlyCollection<TextureMaterialReference> rootReferences,
        IReadOnlyCollection<TextureMaterialManifestEntry> embeddedMaterials,
        CancellationToken cancellationToken)
    {
        var catalogs = await context.AssetCatalogs
            .AsNoTracking()
            .Where(item => item.GameVersion == gameVersion && item.Kind == AssetImportJobValues.Textures && item.IsActive)
            .Select(item => new TextureCatalogHeader(item.Id, item.Kind, item.SchemaVersion, item.MetadataJson))
            .ToListAsync(cancellationToken);

        var gpuTextureFormats = catalogs.Count == 1 && catalogs.All(catalog => catalog.SchemaVersion >= 3)
            ? new[] { "-dxt.ktx" }
            : [];
        if (rootReferences.Count == 0)
        {
            return new StaticMeshMaterialCatalog(
                new StaticMeshMaterialResolver([], []),
                gpuTextureFormats,
                0,
                []);
        }

        var materialGroups = catalogs
            .SelectMany(catalog => JsonSerializer.Deserialize<TextureCatalogMetadata>(catalog.MetadataJson, JsonOptions)?.Materials ?? [])
            .GroupBy(material => Key(material.PackageName, material.ObjectName), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var allMaterials = materialGroups.Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);
        foreach (var material in embeddedMaterials)
            allMaterials[Key(material.PackageName, material.ObjectName)] = material;
        var reachableMaterials = new Dictionary<string, TextureMaterialManifestEntry>(StringComparer.OrdinalIgnoreCase);
        var requiredTextures = new Dictionary<string, TextureMaterialReference>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in rootReferences)
        {
            CollectDependencies(root, string.Empty, allMaterials, reachableMaterials, requiredTextures);
        }

        var loadedTextureEntries = (await LoadTextureEntriesAsync(
            context,
            catalogs,
            requiredTextures.Values.ToArray(),
            cancellationToken)).ToList();
        var requestedTextureKeys = requiredTextures.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var detailReferences = loadedTextureEntries
                .Select(texture => texture.Detail)
                .OfType<TextureMaterialReference>()
                .Where(reference => requestedTextureKeys.Add(Key(reference.PackageName, reference.ObjectName)))
                .ToArray();
            if (detailReferences.Length == 0) break;
            loadedTextureEntries.AddRange(await LoadTextureEntriesAsync(
                context,
                catalogs,
                detailReferences,
                cancellationToken));
        }
        var textureGroups = loadedTextureEntries
            .GroupBy(texture => Key(texture.PackageName, texture.ObjectName), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var textureEntries = textureGroups.Where(group => group.Count() == 1).Select(group => group.Single()).ToArray();
        var embeddedKeys = embeddedMaterials.Select(material => Key(material.PackageName, material.ObjectName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var warnings = materialGroups.Where(group => group.Count() > 1 && !embeddedKeys.Contains(group.Key))
            .Select(group => $"Material '{DisplayKey(group.Key)}' is ambiguous across multiple uploaded source files.")
            .Concat(textureGroups.Where(group => group.Count() > 1)
                .Select(group => $"Texture '{DisplayKey(group.Key)}' is ambiguous across multiple uploaded source files."))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new StaticMeshMaterialCatalog(
            new StaticMeshMaterialResolver(textureEntries, reachableMaterials.Values),
            gpuTextureFormats,
            textureEntries.Length,
            warnings);
    }

    internal static IReadOnlyCollection<TextureMaterialReference> RequiredTextures(
        IReadOnlyCollection<TextureMaterialReference> rootReferences,
        IReadOnlyCollection<TextureMaterialManifestEntry> materials)
    {
        var allMaterials = materials
            .GroupBy(material => Key(material.PackageName, material.ObjectName), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);
        var reachableMaterials = new Dictionary<string, TextureMaterialManifestEntry>(StringComparer.OrdinalIgnoreCase);
        var requiredTextures = new Dictionary<string, TextureMaterialReference>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in rootReferences)
        {
            CollectDependencies(root, string.Empty, allMaterials, reachableMaterials, requiredTextures);
        }
        return requiredTextures.Values.ToArray();
    }

    private static void CollectDependencies(
        TextureMaterialReference reference,
        string currentPackage,
        IReadOnlyDictionary<string, TextureMaterialManifestEntry> allMaterials,
        IDictionary<string, TextureMaterialManifestEntry> reachableMaterials,
        IDictionary<string, TextureMaterialReference> requiredTextures)
    {
        var normalized = Normalize(reference, currentPackage);
        var key = Key(normalized.PackageName, normalized.ObjectName);
        if (!allMaterials.TryGetValue(key, out var material))
        {
            requiredTextures.TryAdd(key, normalized);
            return;
        }
        if (!reachableMaterials.TryAdd(key, material)) return;

        var innerReference = material.ClassName switch
        {
            "FinalBlend" or "Panner" or "Rotator" or "TexPanner" or "TexRotator" or "Combiner" or "TexOscillator" or "TexOscillatorTriggered" or "ColorModifier" => material.Material,
            "FadeColor" => null,
            _ => material.Diffuse
        };
        if (innerReference is not null)
        {
            CollectDependencies(innerReference, material.PackageName, allMaterials, reachableMaterials, requiredTextures);
        }
        CollectDependency(material.Opacity, material.PackageName, allMaterials, reachableMaterials, requiredTextures);
        CollectDependency(material.Mask, material.PackageName, allMaterials, reachableMaterials, requiredTextures);
        CollectDependency(material.SelfIllumination, material.PackageName, allMaterials, reachableMaterials, requiredTextures);
        CollectDependency(material.SelfIlluminationMask, material.PackageName, allMaterials, reachableMaterials, requiredTextures);
        CollectDependency(material.Specular, material.PackageName, allMaterials, reachableMaterials, requiredTextures);
        CollectDependency(material.SpecularityMask, material.PackageName, allMaterials, reachableMaterials, requiredTextures);
        CollectDependency(material.Detail, material.PackageName, allMaterials, reachableMaterials, requiredTextures);
        if (material.ClassName == "Combiner" && material.Material2 is not null)
        {
            CollectDependencies(material.Material2, material.PackageName, allMaterials, reachableMaterials, requiredTextures);
        }
    }

    private static void CollectDependency(
        TextureMaterialReference? reference,
        string currentPackage,
        IReadOnlyDictionary<string, TextureMaterialManifestEntry> allMaterials,
        IDictionary<string, TextureMaterialManifestEntry> reachableMaterials,
        IDictionary<string, TextureMaterialReference> requiredTextures)
    {
        if (reference is null) return;
        CollectDependencies(reference, currentPackage, allMaterials, reachableMaterials, requiredTextures);
    }

    private static async Task<IReadOnlyList<TextureManifestEntry>> LoadTextureEntriesAsync(
        GameContentDbContext context,
        IReadOnlyList<TextureCatalogHeader> catalogs,
        IReadOnlyList<TextureMaterialReference> references,
        CancellationToken cancellationToken)
    {
        if (catalogs.Count == 0 || references.Count == 0) return [];

        var rows = new List<AssetCatalogItem>();
        var catalogIds = catalogs.Select(catalog => catalog.Id).ToArray();
        foreach (var batch in references.Chunk(LookupBatchSize))
        {
            var packageNames = batch.Select(reference => reference.PackageName).ToArray();
            var objectNames = batch.Select(reference => reference.ObjectName).ToArray();
            var catalogIdsParameter = new NpgsqlParameter("catalog_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid)
            {
                Value = catalogIds
            };
            var packageNamesParameter = new NpgsqlParameter("package_names", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = packageNames
            };
            var objectNamesParameter = new NpgsqlParameter("object_names", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = objectNames
            };
            rows.AddRange(await context.AssetCatalogItems
                .FromSqlRaw(
                    "SELECT item.* FROM content.asset_catalog_items AS item " +
                    "INNER JOIN unnest(@package_names::text[], @object_names::text[]) " +
                    "AS wanted(group_name, name) " +
                    "ON lower(item.group_name) = lower(wanted.group_name) AND lower(item.name) = lower(wanted.name) " +
                    "WHERE item.catalog_id = ANY(@catalog_ids::uuid[])",
                    packageNamesParameter,
                    objectNamesParameter,
                    catalogIdsParameter)
                .AsNoTracking()
                .ToListAsync(cancellationToken));
        }

        return catalogs
            .SelectMany(catalog => rows.Where(row => row.CatalogId == catalog.Id))
            .Select(row => JsonSerializer.Deserialize<TextureManifestEntry>(row.MetadataJson, JsonOptions)!)
            .ToArray();
    }

    private static TextureMaterialReference Normalize(TextureMaterialReference reference, string currentPackage) =>
        string.IsNullOrEmpty(reference.PackageName)
            ? reference with { PackageName = currentPackage }
            : reference;

    private static string Key(string packageName, string objectName) => $"{packageName}\n{objectName}";
    private static string DisplayKey(string key) => key.Replace('\n', '.');

    private sealed record TextureCatalogHeader(Guid Id, string Kind, int SchemaVersion, string MetadataJson);
}

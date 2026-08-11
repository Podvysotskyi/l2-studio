using System.Text.Json;
using System.Text.Json.Nodes;

namespace L2.Studio.Repositories.Interfaces.Models;

public static class AssetCatalogMetadataAggregator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Aggregate(string kind, IEnumerable<string> sourceMetadata)
    {
        var metadata = sourceMetadata.Select(value => JsonNode.Parse(value)).OfType<JsonObject>().ToArray();
        if (metadata.Length == 0) return "{}";
        if (kind is AssetImportJobValues.SystemTextures or AssetImportJobValues.Textures)
        {
            var materials = metadata.SelectMany(item => item["materials"]?.AsArray() ?? [])
                .Select(item => item?.DeepClone()).ToArray();
            return JsonSerializer.Serialize(new { materials }, JsonOptions);
        }
        if (kind == AssetImportJobValues.StaticMeshes)
        {
            var formats = metadata.SelectMany(item => item["gpuTextureFormats"]?.AsArray() ?? [])
                .Select(item => item?.GetValue<string>())
                .Where(item => item is not null)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            return JsonSerializer.Serialize(new { gpuTextureFormats = formats }, JsonOptions);
        }
        if (kind == AssetImportJobValues.LevelPreviews)
        {
            var rendererVersion = metadata.Select(item => item["rendererVersion"]?.GetValue<int>() ?? 0).Min();
            return JsonSerializer.Serialize(new { rendererVersion }, JsonOptions);
        }
        return metadata[^1].ToJsonString(JsonOptions);
    }
}

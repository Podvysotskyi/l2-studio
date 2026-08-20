using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Contracts;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Repositories;

public sealed partial class ItemRepository
{
    public async Task<IReadOnlyList<ItemIconSummary>> ResolveItemIconsAsync(
        string gameVersion,
        IReadOnlyList<ItemIconReference> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0) return [];

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entries = await context.AssetCatalogItems.AsNoTracking()
            .Where(item => item.Catalog.GameVersion == gameVersion &&
                item.Catalog.Kind == "textures" && item.Catalog.IsActive &&
                item.GroupName == "Icon" && item.Status == "resolved")
            .Select(item => new { item.Name, item.MetadataJson })
            .ToListAsync(cancellationToken);
        var candidates = entries
            .Select(entry => new { entry.Name, Url = Url(entry.MetadataJson) })
            .Where(entry => entry.Url is not null)
            .Select(entry => new ItemIconCandidate(entry.Name, ExportSuffix(entry.Name), entry.Url!))
            .ToArray();

        return items
            .Select(item => new { Item = item, Candidate = Resolve(candidates, item) })
            .Where(match => match.Candidate is not null)
            .Select(match => new ItemIconSummary(match.Item.ItemId, match.Candidate!.Url))
            .ToArray();
    }

    private static ItemIconCandidate? Resolve(IReadOnlyList<ItemIconCandidate> candidates, ItemIconReference item)
    {
        var matches = candidates.Where(candidate => string.Equals(
            candidate.Suffix, item.Icon["icon.".Length..], StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length == 1) return matches[0];

        var folder = item.ItemBodyPartName?.ToLowerInvariant() switch
        {
            "chest" => "upbody_i",
            "legs" => "lowbody_i",
            _ => null
        };
        return folder is null
            ? null
            : matches.SingleOrDefault(candidate => string.Equals(
                ExportFolder(candidate.Name), folder, StringComparison.OrdinalIgnoreCase));
    }

    private static string ExportSuffix(string name)
    {
        var separator = name.IndexOf('.');
        return separator < 0 ? name : name[(separator + 1)..];
    }

    private static string ExportFolder(string name)
    {
        var separator = name.IndexOf('.');
        return separator < 0 ? string.Empty : name[..separator];
    }

    private sealed record ItemIconCandidate(string Name, string Suffix, string Url);

    private static string? Url(string metadataJson)
    {
        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            return document.RootElement.TryGetProperty("url", out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

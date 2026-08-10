using System.Text.Json;
using L2.Studio.Content;
using L2.Studio.Contracts;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Api.Assets;

public sealed class AssetCatalogRepository(IDbContextFactory<GameContentDbContext> contextFactory)
{
    public async Task<IReadOnlyList<AssetCatalogSummary>> GetSummariesAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AssetCatalogs.AsNoTracking().Where(catalog => catalog.IsActive)
            .OrderBy(catalog => catalog.Kind)
            .Select(catalog => new AssetCatalogSummary(
                catalog.Kind, catalog.SourceFolder, catalog.SourceHash, catalog.SchemaVersion, catalog.Protocol,
                catalog.Items.Count, catalog.Items.LongCount(item => item.Status == "resolved"),
                catalog.Items.LongCount(item => item.Status == "skipped"), catalog.Groups.Count, catalog.PublishedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AssetCatalogPage?> SearchAsync(
        string kind, string query, string? groupName, int page, int pageSize, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var catalog = await context.AssetCatalogs.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Kind == kind && item.IsActive, cancellationToken);
        if (catalog is null) return null;
        var items = context.AssetCatalogItems.AsNoTracking().Where(item => item.CatalogId == catalog.Id);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{EscapeLikePattern(query.Trim())}%";
            items = items.Where(item => EF.Functions.ILike(item.Name, pattern, "\\") ||
                (item.GroupName != null && EF.Functions.ILike(item.GroupName, pattern, "\\")));
        }
        if (!string.IsNullOrWhiteSpace(groupName)) items = items.Where(item => item.GroupName == groupName);
        var total = await items.LongCountAsync(cancellationToken);
        var json = await items.OrderBy(item => item.GroupName).ThenBy(item => item.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(item => item.MetadataJson).ToListAsync(cancellationToken);
        var groups = await context.AssetCatalogGroups.AsNoTracking().Where(item => item.CatalogId == catalog.Id)
            .OrderBy(item => item.Name).Select(item => item.MetadataJson).ToListAsync(cancellationToken);
        var summary = await SummaryAsync(context, catalog.Id, cancellationToken);
        return new AssetCatalogPage(summary, groups.Select(Parse).ToArray(), json.Select(Parse).ToArray(), total, page, pageSize);
    }

    public async Task<JsonElement?> GetAsync(string kind, string name, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var json = await context.AssetCatalogItems.AsNoTracking()
            .Where(item => item.Catalog.Kind == kind && item.Catalog.IsActive && item.Name == name)
            .Select(item => item.MetadataJson).FirstOrDefaultAsync(cancellationToken);
        return json is null ? null : Parse(json);
    }

    private static async Task<AssetCatalogSummary> SummaryAsync(GameContentDbContext context, Guid id, CancellationToken token) =>
        await context.AssetCatalogs.AsNoTracking().Where(catalog => catalog.Id == id)
            .Select(catalog => new AssetCatalogSummary(catalog.Kind, catalog.SourceFolder, catalog.SourceHash,
                catalog.SchemaVersion, catalog.Protocol, catalog.Items.Count,
                catalog.Items.LongCount(item => item.Status == "resolved"), catalog.Items.LongCount(item => item.Status == "skipped"),
                catalog.Groups.Count, catalog.PublishedAt)).SingleAsync(token);

    private static JsonElement Parse(string json) => JsonSerializer.Deserialize<JsonElement>(json);
    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
}

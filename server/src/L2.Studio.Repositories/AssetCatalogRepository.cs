using System.Text.Json;
using System.Security.Cryptography;
using System.Globalization;
using L2.Studio.Context;
using L2.Studio.Contracts;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace L2.Studio.Repositories;

public sealed class AssetCatalogRepository(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider)
    : IAssetCatalogRepository
{
    public async Task<IReadOnlyList<AssetCatalogSummary>> GetSummariesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AssetCatalogs.AsNoTracking().Where(catalog =>
                catalog.GameVersion == gameVersion && catalog.IsActive)
            .OrderBy(catalog => catalog.Kind)
            .Select(catalog => new AssetCatalogSummary(
                catalog.Kind, catalog.SourceFolder, catalog.SourceHash, catalog.SchemaVersion, catalog.Protocol,
                catalog.Items.Count, catalog.Items.LongCount(item => item.Status == "resolved"),
                catalog.Items.LongCount(item => item.Status == "skipped"), catalog.Groups.Count, catalog.PublishedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<NpcAppearanceManifestReference?> GetNpcAppearanceManifestAsync(
        string gameVersion,
        int npcId,
        CancellationToken cancellationToken)
    {
        if (npcId < 0) return null;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var catalog = await context.AssetCatalogs.AsNoTracking()
            .Where(catalog => catalog.GameVersion == gameVersion &&
                catalog.Kind == AssetImportJobValues.NpcAppearances && catalog.IsActive)
            .Select(catalog => new { catalog.SchemaVersion, catalog.MetadataJson })
            .SingleOrDefaultAsync(cancellationToken);
        if (catalog is null || catalog.SchemaVersion < 6) return null;

        using var document = JsonDocument.Parse(catalog.MetadataJson);
        var metadata = document.RootElement;
        if (!metadata.TryGetProperty("npcManifestUrlTemplate", out var manifestUrlTemplate) ||
            manifestUrlTemplate.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(manifestUrlTemplate.GetString()))
            return null;
        if (!metadata.TryGetProperty("npcIds", out var npcIds) ||
            npcIds.ValueKind != JsonValueKind.Array)
            return null;
        if (!npcIds.EnumerateArray().Any(value =>
                value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var id) && id == npcId))
            return null;

        return new NpcAppearanceManifestReference(
            manifestUrlTemplate.GetString()!.Replace("{id}", npcId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
    }

    public async Task<AssetCatalogPage?> SearchAsync(
        string gameVersion, string kind, string query, string? groupName, string? originalFolder, int page, int pageSize, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var catalog = await context.AssetCatalogs.AsNoTracking()
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion && item.Kind == kind && item.IsActive, cancellationToken);
        if (catalog is null) return null;
        var items = context.AssetCatalogItems.AsNoTracking().Where(item => item.CatalogId == catalog.Id);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{EscapeLikePattern(query.Trim())}%";
            items = items.Where(item => EF.Functions.ILike(item.Name, pattern, "\\") ||
                (item.GroupName != null && EF.Functions.ILike(item.GroupName, pattern, "\\")) ||
                EF.Functions.ILike(item.Source.SourceKey, pattern, "\\"));
        }
        if (!string.IsNullOrWhiteSpace(groupName)) items = items.Where(item => item.GroupName == groupName);
        if (!string.IsNullOrWhiteSpace(originalFolder))
        {
            var prefix = $"{originalFolder.Trim().ToLowerInvariant()}/";
            items = items.Where(item => item.Source.NormalizedSourceKey.StartsWith(prefix));
        }
        var total = await items.LongCountAsync(cancellationToken);
        var json = await items.OrderBy(item => item.GroupName).ThenBy(item => item.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(item => item.MetadataJson).ToListAsync(cancellationToken);
        var groups = await context.AssetCatalogGroups.AsNoTracking().Where(item => item.CatalogId == catalog.Id &&
                (string.IsNullOrWhiteSpace(originalFolder) || item.Source.NormalizedSourceKey.StartsWith(originalFolder.Trim().ToLowerInvariant() + "/")))
            .OrderBy(item => item.Name).Select(item => item.MetadataJson).ToListAsync(cancellationToken);
        var summary = await SummaryAsync(context, catalog.Id, cancellationToken);
        return new AssetCatalogPage(summary, groups.Select(Parse).ToArray(), json.Select(Parse).ToArray(), total, page, pageSize);
    }

    public async Task<JsonElement?> GetAsync(
        string gameVersion,
        string kind,
        string name,
        string? sourceKey,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = context.AssetCatalogItems.AsNoTracking()
            .Where(item => item.Catalog.GameVersion == gameVersion && item.Catalog.Kind == kind &&
                item.Catalog.IsActive && item.Name == name);
        if (!string.IsNullOrWhiteSpace(sourceKey))
        {
            var normalizedSourceKey = sourceKey.Trim().ToLowerInvariant();
            items = items.Where(item => item.Source.NormalizedSourceKey == normalizedSourceKey);
        }
        var json = await items.Select(item => item.MetadataJson).Take(2).ToArrayAsync(cancellationToken);
        if (json.Length > 1)
            throw new InvalidOperationException($"Catalog entry '{name}' is ambiguous; provide its source key.");
        return json.Length == 0 ? null : Parse(json[0]);
    }

    public async Task<AssetCatalogDiagnosticPage?> GetDiagnosticsAsync(
        string gameVersion,
        string kind,
        string name,
        string? sourceKey,
        string? severity,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var catalogItems = context.AssetCatalogItems.AsNoTracking()
            .Where(item => item.Catalog.GameVersion == gameVersion && item.Catalog.Kind == kind &&
                item.Catalog.IsActive && item.Name == name);
        if (!string.IsNullOrWhiteSpace(sourceKey))
        {
            var normalizedSourceKey = sourceKey.Trim().ToLowerInvariant();
            catalogItems = catalogItems.Where(item => item.Source.NormalizedSourceKey == normalizedSourceKey);
        }
        var targets = await catalogItems.Select(item => new
            {
                item.Source.PublishingWorkItemId,
                item.Source.SourceKey,
                item.Source.PublishedAt
            })
            .Take(2)
            .ToArrayAsync(cancellationToken);
        if (targets.Length > 1)
            throw new InvalidOperationException($"Catalog entry '{name}' is ambiguous; provide its source key.");
        if (targets.Length == 0) return null;

        var target = targets[0];
        var workItem = await context.AssetImportWorkItems.AsNoTracking()
            .Where(item => item.Id == target.PublishingWorkItemId)
            .Select(item => new { item.Id, item.RunId, item.Status })
            .SingleAsync(cancellationToken);
        var diagnostics = context.AssetImportDiagnostics.AsNoTracking()
            .Where(item => item.WorkItemId == workItem.Id);
        if (!string.IsNullOrWhiteSpace(severity))
            diagnostics = diagnostics.Where(item => item.Severity == severity);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var search = query.Trim();
            diagnostics = diagnostics.Where(item =>
                EF.Functions.ToTsVector("simple",
                    (item.SourceKey ?? string.Empty) + " " + (item.ObjectName ?? string.Empty) + " " + item.Message)
                    .Matches(EF.Functions.WebSearchToTsQuery("simple", search)));
        }
        var total = await diagnostics.LongCountAsync(cancellationToken);
        var items = await diagnostics.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new AssetImportDiagnosticSummary(
                item.Id, item.RunId, item.WorkItemId, item.Severity, item.Code, item.Stage,
                item.SourceKey, item.ObjectName, item.Message, item.CreatedAt))
            .ToListAsync(cancellationToken);
        return new AssetCatalogDiagnosticPage(
            workItem.RunId,
            workItem.Id,
            target.SourceKey,
            workItem.Status,
            target.PublishedAt,
            items,
            total,
            page,
            pageSize);
    }

    public async Task<AssetArtifactPage> GetArtifactsAsync(
        string gameVersion,
        string? kind,
        string? sourceKey,
        bool? current,
        string? integrityStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var artifacts = context.AssetArtifacts.AsNoTracking().Where(item => item.GameVersion == gameVersion);
        if (!string.IsNullOrWhiteSpace(kind)) artifacts = artifacts.Where(item => item.Kind == kind);
        if (!string.IsNullOrWhiteSpace(sourceKey))
        {
            var pattern = $"%{EscapeLikePattern(sourceKey.Trim())}%";
            artifacts = artifacts.Where(item => EF.Functions.ILike(item.SourceKey, pattern, "\\"));
        }
        if (!string.IsNullOrWhiteSpace(integrityStatus))
            artifacts = artifacts.Where(item => item.IntegrityStatus == integrityStatus);
        if (current is not null)
            artifacts = current.Value
                ? artifacts.Where(item => item.Publications.Any())
                : artifacts.Where(item => !item.Publications.Any());
        var total = await artifacts.LongCountAsync(cancellationToken);
        var items = await artifacts.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.SourceKey)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new AssetArtifactSummary(
                item.Id, item.Kind, item.SourceKey, item.SourceHash, item.RecipeVersion,
                item.BuildFingerprint, item.ContentHash, item.OutputRoot, item.SchemaVersion,
                item.Protocol, item.FileCount, item.SizeBytes, item.IntegrityStatus,
                item.LastVerifiedAt, item.CreatedAt, item.Publications.Any()))
            .ToListAsync(cancellationToken);
        return new AssetArtifactPage(items, total, page, pageSize);
    }

    public async Task<AssetArtifactDetail?> GetArtifactAsync(
        string gameVersion,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var artifact = await context.AssetArtifacts.AsNoTracking().Include(item => item.Files)
            .Include(item => item.Dependencies)
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion && item.Id == id, cancellationToken);
        if (artifact is null) return null;
        var summary = new AssetArtifactSummary(
            artifact.Id, artifact.Kind, artifact.SourceKey, artifact.SourceHash, artifact.RecipeVersion,
            artifact.BuildFingerprint, artifact.ContentHash, artifact.OutputRoot, artifact.SchemaVersion,
            artifact.Protocol, artifact.FileCount, artifact.SizeBytes, artifact.IntegrityStatus,
            artifact.LastVerifiedAt, artifact.CreatedAt,
            await context.AssetCatalogSources.AsNoTracking().AnyAsync(item => item.ArtifactId == artifact.Id, cancellationToken));
        return new AssetArtifactDetail(
            summary,
            artifact.Files.OrderBy(file => file.RelativePath).Select(file => new AssetArtifactFileSummary(
                file.RelativePath, file.PublicPath, file.Role, file.MediaType, file.SizeBytes, file.Sha256)).ToArray(),
            artifact.Dependencies.OrderBy(item => item.Kind).ThenBy(item => item.DependencyKey)
                .Select(item => new AssetArtifactDependencySummary(
                    item.Kind, item.DependencyKey, item.ResolvedArtifactId, item.ResolvedSourceKey,
                    item.BuildFingerprint, item.IsResolved)).ToArray());
    }

    public async Task<AssetArtifactDetail?> VerifyArtifactAsync(
        string gameVersion,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var artifact = await context.AssetArtifacts.Include(item => item.Files)
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion && item.Id == id, cancellationToken);
        if (artifact is null) return null;
        var root = ContainedPath(Path.GetFullPath(options.Value.AssetRootPath), artifact.OutputRoot);
        var marker = Path.Combine(root, ".l2-asset-version");
        var healthy = Directory.Exists(root) && File.Exists(Path.Combine(root, ".l2-artifact.json")) &&
            File.Exists(marker) && string.Equals(
                (await File.ReadAllTextAsync(marker, cancellationToken)).Trim(),
                artifact.BuildFingerprint, StringComparison.Ordinal);
        foreach (var file in artifact.Files)
        {
            if (!healthy) break;
            var path = ContainedPath(root, file.RelativePath);
            if (!File.Exists(path) || new FileInfo(path).Length != file.SizeBytes)
            {
                healthy = false;
                break;
            }
            await using var stream = File.OpenRead(path);
            var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
            if (!string.Equals(hash, file.Sha256, StringComparison.Ordinal)) healthy = false;
        }
        artifact.IntegrityStatus = healthy ? "healthy" : Directory.Exists(root) ? "corrupt" : "missing";
        artifact.LastVerifiedAt = timeProvider.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);
        return await GetArtifactAsync(gameVersion, id, cancellationToken);
    }

    private static async Task<AssetCatalogSummary> SummaryAsync(GameContentDbContext context, Guid id, CancellationToken token) =>
        await context.AssetCatalogs.AsNoTracking().Where(catalog => catalog.Id == id)
            .Select(catalog => new AssetCatalogSummary(catalog.Kind, catalog.SourceFolder, catalog.SourceHash,
                catalog.SchemaVersion, catalog.Protocol, catalog.Items.Count,
                catalog.Items.LongCount(item => item.Status == "resolved"), catalog.Items.LongCount(item => item.Status == "skipped"),
                catalog.Groups.Count, catalog.PublishedAt)).SingleAsync(token);

    private static JsonElement Parse(string json) => JsonSerializer.Deserialize<JsonElement>(json);
    private static string ContainedPath(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) throw new InvalidDataException("Artifact paths must be relative.");
        var path = Path.GetFullPath(Path.Combine(root, relativePath));
        var relative = Path.GetRelativePath(root, path);
        if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal))
            throw new InvalidDataException("Artifact path escaped the configured root.");
        return path;
    }
    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
}

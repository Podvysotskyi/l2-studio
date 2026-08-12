using System.Text.RegularExpressions;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Studio.Exceptions;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wolverine.EntityFrameworkCore;

namespace L2.Studio.Repositories;

public sealed partial class AssetImportRepository(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider) : IAssetImportRepository
{
    [GeneratedRegex("^[0-9]{2}_[0-9]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex WorldMapNamePattern();

    public async Task<AssetImportRunSummary?> QueueFullScanAsync(
        string gameVersion,
        string kind,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await AcquireKindLockAsync(context, gameVersion, kind, cancellationToken);
        if (await HasConflictingRunAsync(context, gameVersion, kind, null, cancellationToken)) return null;

        var run = new AssetImportRun
        {
            Id = Guid.NewGuid(),
            GameVersion = gameVersion,
            Kind = kind,
            TriggerType = AssetImportJobValues.FullScan,
            Status = AssetImportJobValues.Queued,
            RequestedAt = timeProvider.GetUtcNow()
        };
        context.AssetImportRuns.Add(run);
        outbox.Enroll(context);
        await outbox.PublishAsync(DiscoveryCommand(kind, run.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return ToSummary(run);
    }

    public async Task<AssetImportRunSummary?> QueueSingleFileAsync(
        string gameVersion,
        string kind,
        string fileName,
        CancellationToken cancellationToken)
    {
        var source = await ValidateSingleFileAsync(gameVersion, kind, fileName, cancellationToken);
        var normalized = NormalizeSourceKey(source.FileName);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await AcquireKindLockAsync(context, gameVersion, kind, cancellationToken);
        if (await HasConflictingRunAsync(context, gameVersion, kind, normalized, cancellationToken)) return null;

        var now = timeProvider.GetUtcNow();
        var run = new AssetImportRun
        {
            Id = Guid.NewGuid(),
            GameVersion = gameVersion,
            Kind = kind,
            TriggerType = AssetImportJobValues.SingleFile,
            Status = AssetImportJobValues.Queued,
            RequestedSourceKey = source.FileName,
            NormalizedRequestedSourceKey = normalized,
            RequestedAt = now,
            DiscoveryFinishedAt = now,
            DiscoveredFileCount = 1
        };
        var item = new AssetImportWorkItem
        {
            Id = Guid.NewGuid(),
            GameVersion = gameVersion,
            RunId = run.Id,
            ImportKind = kind,
            SourceKey = source.FileName,
            NormalizedSourceKey = normalized,
            SourcePath = source.FullPath,
            SourceHash = source.SourceHash,
            Status = AssetImportJobValues.Queued,
            CreatedAt = now
        };
        run.WorkItems.Add(item);
        context.AssetImportRuns.Add(run);
        outbox.Enroll(context);
        await outbox.PublishAsync(FileCommand(kind, item.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return ToSummary(run);
    }

    public async Task<IReadOnlyList<AssetImportRunSummary>> GetRecentAsync(
        string gameVersion,
        string kind,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var runs = await context.AssetImportRuns.AsNoTracking()
            .Where(run => run.GameVersion == gameVersion && run.Kind == kind)
            .OrderByDescending(run => run.RequestedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return runs.Select(ToSummary).ToArray();
    }

    public async Task<AssetImportRunSummary?> GetAsync(
        Guid id,
        string gameVersion,
        string kind,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = await context.AssetImportRuns.AsNoTracking()
            .Where(run => run.Id == id && run.GameVersion == gameVersion && run.Kind == kind)
            .SingleOrDefaultAsync(cancellationToken);
        return run is null ? null : ToSummary(run);
    }

    public async Task<AssetImportWorkItemPage?> GetWorkItemsAsync(
        Guid runId,
        string gameVersion,
        string kind,
        string? sourceKey,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await context.AssetImportRuns.AsNoTracking().AnyAsync(
                run => run.Id == runId && run.GameVersion == gameVersion && run.Kind == kind, cancellationToken)) return null;
        var query = context.AssetImportWorkItems.AsNoTracking().Where(item => item.RunId == runId);
        if (!string.IsNullOrWhiteSpace(sourceKey))
            query = query.Where(item => EF.Functions.ILike(item.SourceKey, $"%{EscapeLike(sourceKey.Trim())}%", "\\"));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.SourceKey)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new AssetImportWorkItemSummary(
                item.Id, item.RunId, item.ImportKind, item.SourceKey, item.SourcePath, item.SourceHash,
                item.Status, item.AttemptCount, item.CreatedAt, item.StartedAt, item.FinishedAt,
                item.TotalResourceCount, item.ProcessedResourceCount, item.SkippedResourceCount,
                item.WarningCount, item.Error, item.UnpublishedAt))
            .ToListAsync(cancellationToken);
        return new AssetImportWorkItemPage(items, total, page, pageSize);
    }

    public async Task<AssetImportDiagnosticPage?> GetDiagnosticsAsync(
        Guid runId,
        string gameVersion,
        string kind,
        string? sourceKey,
        string? severity,
        string? code,
        string? stage,
        string? workItemStatus,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await context.AssetImportRuns.AsNoTracking().AnyAsync(
                run => run.Id == runId && run.GameVersion == gameVersion && run.Kind == kind, cancellationToken)) return null;
        var diagnostics = context.AssetImportDiagnostics.AsNoTracking().Where(item => item.RunId == runId);
        if (!string.IsNullOrWhiteSpace(sourceKey)) diagnostics = diagnostics.Where(item => item.SourceKey == sourceKey);
        if (!string.IsNullOrWhiteSpace(severity)) diagnostics = diagnostics.Where(item => item.Severity == severity);
        if (!string.IsNullOrWhiteSpace(code)) diagnostics = diagnostics.Where(item => item.Code == code);
        if (!string.IsNullOrWhiteSpace(stage)) diagnostics = diagnostics.Where(item => item.Stage == stage);
        if (!string.IsNullOrWhiteSpace(workItemStatus))
            diagnostics = diagnostics.Where(item => item.WorkItem != null && item.WorkItem.Status == workItemStatus);
        if (!string.IsNullOrWhiteSpace(query))
        {
            diagnostics = diagnostics.Where(item =>
                EF.Functions.ToTsVector("simple",
                    (item.SourceKey ?? string.Empty) + " " + (item.ObjectName ?? string.Empty) + " " + item.Message)
                    .Matches(EF.Functions.WebSearchToTsQuery("simple", query.Trim())));
        }
        var total = await diagnostics.LongCountAsync(cancellationToken);
        var items = await diagnostics.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new AssetImportDiagnosticSummary(
                item.Id, item.RunId, item.WorkItemId, item.Severity, item.Code, item.Stage,
                item.SourceKey, item.ObjectName, item.Message, item.CreatedAt))
            .ToListAsync(cancellationToken);
        return new AssetImportDiagnosticPage(items, total, page, pageSize);
    }

    private async Task<ValidatedSource> ValidateSingleFileAsync(
        string gameVersion,
        string kind,
        string fileName,
        CancellationToken cancellationToken)
    {
        var normalizedFileName = fileName.Replace('\\', '/');
        var extension = Path.GetExtension(normalizedFileName);
        var expected = kind == AssetImportJobValues.Music ? ".ogg" : kind switch
        {
            AssetImportJobValues.Textures => ".utx",
            AssetImportJobValues.StaticMeshes => ".usx",
            AssetImportJobValues.Sounds => ".uax",
            AssetImportJobValues.Maps or AssetImportJobValues.Scenes or AssetImportJobValues.MapPreviews => ".unr",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (!string.Equals(extension, expected, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"The '{kind}' import requires a {expected} file.", nameof(fileName));
        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (kind is AssetImportJobValues.Maps or AssetImportJobValues.MapPreviews && !WorldMapNamePattern().IsMatch(stem))
            throw new ArgumentException("The file is not a coordinate-named world map.", nameof(fileName));
        if (kind == AssetImportJobValues.Scenes && WorldMapNamePattern().IsMatch(stem))
            throw new ArgumentException("The file is a world map, not a client scene.", nameof(fileName));

        var root = Path.GetFullPath(SourceRoot(gameVersion, kind));
        if (kind == AssetImportJobValues.Textures)
        {
            var segments = normalizedFileName.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2 || segments[0] is not "systextures" and not "textures")
                throw new ArgumentException("Texture imports require a folder-qualified source key such as 'systextures/Interface.utx'.", nameof(fileName));
            root = Path.Combine(Path.GetFullPath(options.Value.SourceRootPath), SourceFolder(gameVersion), segments[0]);
            normalizedFileName = segments[1];
        }
        string fullPath;
        try
        {
            fullPath = AssetImportPathValidator.ResolveContainedFile(root, normalizedFileName, expected);
        }
        catch (FileNotFoundException)
        {
            throw new AssetImportTargetNotFoundException(fileName);
        }

        string sourceHash;
        if (kind == AssetImportJobValues.MapPreviews)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var normalized = NormalizeSourceKey(Path.GetFileName(fullPath));
            var mapSourceHash = await context.AssetCatalogSources.AsNoTracking().Where(source =>
                source.Catalog.GameVersion == gameVersion && source.Catalog.Kind == AssetImportJobValues.Maps && source.Catalog.IsActive &&
                source.NormalizedSourceKey == normalized)
                .Select(source => source.SourceHash)
                .SingleOrDefaultAsync(cancellationToken);
            if (mapSourceHash is null) throw new AssetImportTargetNotFoundException(fileName);
            sourceHash = AssetImportSourceHash.MapPreview(mapSourceHash);
        }
        else
        {
            sourceHash = await AssetImportSourceHash.FileAsync(fullPath, cancellationToken);
        }
        var sourceKey = kind == AssetImportJobValues.Textures
            ? $"{fileName.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)[0]}/{Path.GetFileName(fullPath)}"
            : Path.GetFileName(fullPath);
        return new ValidatedSource(sourceKey, fullPath, sourceHash);
    }

    private string SourceRoot(string gameVersion, string kind) => Path.Combine(
        options.Value.SourceRootPath,
        SourceFolder(gameVersion),
        kind switch
    {
        AssetImportJobValues.Maps or AssetImportJobValues.MapPreviews or AssetImportJobValues.Scenes => "maps",
        var value => value
    });

    private static string SourceFolder(string gameVersion) => gameVersion switch
    {
        "c1" => "C1",
        "c4" => "C4",
        "interlude" => "Interlude",
        _ => throw new ArgumentOutOfRangeException(nameof(gameVersion))
    };

    private static async Task AcquireKindLockAsync(
        GameContentDbContext context,
        string gameVersion,
        string kind,
        CancellationToken token)
    {
        var key = $"l2-asset-import:{gameVersion}:{kind}";
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({key}))", token);
    }

    private static Task<bool> HasConflictingRunAsync(
        GameContentDbContext context,
        string gameVersion,
        string kind,
        string? normalizedSourceKey,
        CancellationToken token) =>
        context.AssetImportRuns.AnyAsync(run => run.GameVersion == gameVersion && run.Kind == kind &&
            AssetImportJobValues.ActiveStatuses.Contains(run.Status) &&
            (normalizedSourceKey == null || run.TriggerType == AssetImportJobValues.FullScan ||
                run.NormalizedRequestedSourceKey == normalizedSourceKey), token);

    public static string NormalizeSourceKey(string sourceKey) => sourceKey.Trim().ToLowerInvariant();

    private static object DiscoveryCommand(string kind, Guid runId) => kind switch
    {
        AssetImportJobValues.Textures => new DiscoverTextures(runId),
        AssetImportJobValues.StaticMeshes => new DiscoverStaticMeshes(runId),
        AssetImportJobValues.Sounds => new DiscoverSounds(runId),
        AssetImportJobValues.Music => new DiscoverMusic(runId),
        AssetImportJobValues.Maps => new DiscoverMaps(runId),
        AssetImportJobValues.Scenes => new DiscoverScenes(runId),
        AssetImportJobValues.MapPreviews => new DiscoverMapPreviews(runId),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static object FileCommand(string kind, Guid workItemId) => kind switch
    {
        AssetImportJobValues.Textures => new ImportTextureFile(workItemId),
        AssetImportJobValues.StaticMeshes => new ImportStaticMeshFile(workItemId),
        AssetImportJobValues.Sounds => new ImportSoundFile(workItemId),
        AssetImportJobValues.Music => new ImportMusicFile(workItemId),
        AssetImportJobValues.Maps => new ImportMapFile(workItemId),
        AssetImportJobValues.Scenes => new ImportSceneFile(workItemId),
        AssetImportJobValues.MapPreviews => new GenerateMapPreview(workItemId),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static AssetImportRunSummary ToSummary(AssetImportRun run) => new(
        run.Id, run.Kind, run.TriggerType, run.Status, run.RequestedSourceKey, run.RequestedAt,
        run.StartedAt, run.DiscoveryFinishedAt, run.FinishedAt, run.DiscoveredFileCount,
        run.CompletedFileCount, run.SucceededFileCount, run.WarningFileCount,
        run.FailedFileCount, run.Error);

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record ValidatedSource(string FileName, string FullPath, string SourceHash);
}

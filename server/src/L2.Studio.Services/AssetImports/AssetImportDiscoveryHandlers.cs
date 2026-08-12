using L2.Studio.Context;
using System.Data.Common;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wolverine.Attributes;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Services;

[WolverineHandler]
public sealed class AssetImportDiscoveryHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider)
{
    public Task Handle(DiscoverSystemTextures message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.SystemTextures, token);
    public Task Handle(DiscoverTextures message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Textures, token);
    public Task Handle(DiscoverStaticMeshes message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.StaticMeshes, token);
    public Task Handle(DiscoverSounds message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Sounds, token);
    public Task Handle(DiscoverMusic message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Music, token);
    public Task Handle(DiscoverLevels message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Levels, token);
    public Task Handle(DiscoverScenes message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Scenes, token);
    public Task Handle(DiscoverLevelPreviews message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.LevelPreviews, token);

    private async Task DiscoverAsync(Guid runId, string kind, CancellationToken cancellationToken)
    {
        string gameVersion;
        await using (var claimContext = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            var run = await claimContext.AssetImportRuns.SingleOrDefaultAsync(item => item.Id == runId, cancellationToken);
            if (run is null || run.DiscoveryFinishedAt is not null || AssetImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            run.Status = AssetImportJobValues.Discovering;
            gameVersion = run.GameVersion;
            run.StartedAt ??= timeProvider.GetUtcNow();
            await claimContext.SaveChangesAsync(cancellationToken);
        }

        IReadOnlyList<DiscoveredSource> sources;
        try
        {
            sources = kind == AssetImportJobValues.LevelPreviews
                ? await DiscoverPreviewSourcesAsync(gameVersion, cancellationToken)
                : await DiscoverFileSourcesAsync(gameVersion, kind, cancellationToken);
        }
        catch (Exception exception) when (IsDiscoveryFailure(exception))
        {
            await FailDiscoveryAsync(runId, exception, cancellationToken);
            return;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var current = await context.AssetImportRuns.Include(run => run.WorkItems)
            .SingleAsync(run => run.Id == runId, cancellationToken);
        if (current.DiscoveryFinishedAt is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }
        var now = timeProvider.GetUtcNow();
        outbox.Enroll(context);
        foreach (var source in sources)
        {
            var item = new AssetImportWorkItem
            {
                Id = Guid.NewGuid(),
                GameVersion = current.GameVersion,
                RunId = current.Id,
                ImportKind = kind,
                SourceKey = source.SourceKey,
                NormalizedSourceKey = NormalizeSourceKey(source.SourceKey),
                SourcePath = source.SourcePath,
                SourceHash = source.SourceHash,
                Status = source.Error is null ? AssetImportJobValues.Queued : AssetImportJobValues.Failed,
                CreatedAt = now,
                FinishedAt = source.Error is null ? null : now,
                Error = source.Error
            };
            current.WorkItems.Add(item);
            if (source.Error is null)
            {
                await outbox.PublishAsync(FileCommand(kind, item.Id));
            }
            else
            {
                context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
                {
                    RunId = current.Id,
                    WorkItemId = item.Id,
                    Severity = "error",
                    Code = "discovery.hash_failed",
                    Stage = "discovery",
                    SourceKey = item.SourceKey,
                    Message = source.Error,
                    CreatedAt = now
                });
                await outbox.PublishAsync(new AssetImportWorkItemCompleted(current.Id, item.Id));
            }
        }
        current.DiscoveredFileCount = sources.Count;
        current.DiscoveryFinishedAt = now;
        current.Status = AssetImportJobValues.Running;
        if (sources.Count == 0) await outbox.PublishAsync(new FinalizeAssetImportRun(current.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(MultiFlushMode.AllowMultiples, cancellationToken);
    }

    private async Task<IReadOnlyList<DiscoveredSource>> DiscoverFileSourcesAsync(
        string gameVersion,
        string kind,
        CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(SourceRoot(gameVersion, kind));
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"The configured source directory does not exist: {root}");
        var extension = ExpectedExtension(kind);
        var paths = Directory.EnumerateFiles(root)
            .Where(path => string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
            .Where(path => kind switch
            {
                AssetImportJobValues.Levels => UnrealPackageKindClassifier.IsWorldLevel(path),
                AssetImportJobValues.Scenes => UnrealPackageKindClassifier.IsScene(path),
                _ => true
            })
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var duplicate = paths.GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) throw new InvalidDataException($"Source filename '{duplicate.Key}' is duplicated ignoring case.");
        var result = new List<DiscoveredSource>(paths.Length);
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (new FileInfo(path).LinkTarget is not null)
                    throw new InvalidDataException("Symbolic-link sources are not supported.");
                var hash = await AssetImportSourceHash.FileAsync(path, cancellationToken);
                result.Add(new DiscoveredSource(Path.GetFileName(path), Path.GetFullPath(path), hash, null));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                result.Add(new DiscoveredSource(Path.GetFileName(path), Path.GetFullPath(path), null, exception.Message));
            }
        }
        return result;
    }

    private async Task<IReadOnlyList<DiscoveredSource>> DiscoverPreviewSourcesAsync(
        string gameVersion,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var sources = await context.AssetCatalogSources.AsNoTracking()
            .Where(source => source.Catalog.GameVersion == gameVersion &&
                source.Catalog.Kind == AssetImportJobValues.Levels && source.Catalog.IsActive)
            .OrderBy(source => source.SourceKey)
            .Select(source => new { source.SourceKey, source.SourceHash })
            .ToListAsync(cancellationToken);
        var root = Path.GetFullPath(SourceRoot(gameVersion, AssetImportJobValues.Levels));
        return sources.Select(source => new DiscoveredSource(
            source.SourceKey,
            Path.Combine(root, source.SourceKey),
            LevelPreviewGeneration.ComputeSourceHash(source.SourceHash),
            null)).ToArray();
    }

    private async Task FailDiscoveryAsync(Guid runId, Exception exception, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = await context.AssetImportRuns.SingleAsync(item => item.Id == runId, cancellationToken);
        if (AssetImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        var now = timeProvider.GetUtcNow();
        run.Status = AssetImportJobValues.Failed;
        run.DiscoveryFinishedAt = now;
        run.FinishedAt = now;
        run.Error = Truncate(exception.Message);
        context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
        {
            RunId = run.Id,
            Severity = "error",
            Code = "discovery.failed",
            Stage = "discovery",
            Message = Truncate(exception.Message),
            CreatedAt = now
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private string SourceRoot(string gameVersion, string kind) => Path.Combine(
        options.Value.SourceRootPath,
        SourceFolder(gameVersion),
        kind switch
    {
        AssetImportJobValues.Levels or AssetImportJobValues.Scenes => "maps",
        var value => value
    });

    private static string SourceFolder(string gameVersion) => gameVersion switch
    {
        "c1" => "C1",
        "c4" => "C4",
        "interlude" => "Interlude",
        _ => throw new ArgumentOutOfRangeException(nameof(gameVersion))
    };

    private static string ExpectedExtension(string kind) => kind switch
    {
        AssetImportJobValues.SystemTextures or AssetImportJobValues.Textures => ".utx",
        AssetImportJobValues.StaticMeshes => ".usx",
        AssetImportJobValues.Sounds => ".uax",
        AssetImportJobValues.Music => ".ogg",
        AssetImportJobValues.Levels or AssetImportJobValues.Scenes => ".unr",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static object FileCommand(string kind, Guid id) => kind switch
    {
        AssetImportJobValues.SystemTextures => new ImportSystemTextureFile(id),
        AssetImportJobValues.Textures => new ImportTextureFile(id),
        AssetImportJobValues.StaticMeshes => new ImportStaticMeshFile(id),
        AssetImportJobValues.Sounds => new ImportSoundFile(id),
        AssetImportJobValues.Music => new ImportMusicFile(id),
        AssetImportJobValues.Levels => new ImportLevelFile(id),
        AssetImportJobValues.Scenes => new ImportSceneFile(id),
        AssetImportJobValues.LevelPreviews => new GenerateLevelPreview(id),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string Truncate(string value) => value.Length <= 4000 ? value : value[..4000];
    private static bool IsDiscoveryFailure(Exception exception) =>
        exception is not OperationCanceledException &&
        exception is not DbException &&
        exception is not DbUpdateException &&
        exception.InnerException is not DbException;
    private static string NormalizeSourceKey(string value) => value.Trim().ToLowerInvariant();
    private sealed record DiscoveredSource(string SourceKey, string SourcePath, string? SourceHash, string? Error);
}

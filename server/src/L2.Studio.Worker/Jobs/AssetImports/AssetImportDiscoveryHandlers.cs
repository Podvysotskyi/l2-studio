using L2.Studio.Context;
using System.Data.Common;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services;
using L2.Studio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wolverine.Attributes;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class AssetImportDiscoveryHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider)
{
    public Task Handle(DiscoverTextures message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Textures, token);
    public Task Handle(DiscoverStaticMeshes message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.StaticMeshes, token);
    public Task Handle(DiscoverSounds message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Sounds, token);
    public Task Handle(DiscoverMusic message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Music, token);
    public Task Handle(DiscoverMaps message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Maps, token);
    public Task Handle(DiscoverScenes message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.Scenes, token);
    public Task Handle(DiscoverMapPreviews message, CancellationToken token) =>
        DiscoverAsync(message.RunId, AssetImportJobValues.MapPreviews, token);

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
            run.LastHeartbeatAt = run.StartedAt;
            await claimContext.SaveChangesAsync(cancellationToken);
        }
        await using var heartbeat = AssetImportHeartbeatLease.Start(
            contextFactory, timeProvider, runId, null, cancellationToken);

        IReadOnlyList<DiscoveredSource> sources;
        try
        {
            sources = kind == AssetImportJobValues.MapPreviews
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
        var published = await context.AssetCatalogSources.AsNoTracking().Include(source => source.Dependencies)
            .Where(source => source.Catalog.GameVersion == current.GameVersion && source.Catalog.Kind == kind && source.Catalog.IsActive)
            .ToDictionaryAsync(source => source.NormalizedSourceKey, StringComparer.Ordinal, cancellationToken);
        outbox.Enroll(context);
        foreach (var source in sources)
        {
            var normalizedSourceKey = NormalizeSourceKey(source.SourceKey);
            published.TryGetValue(normalizedSourceKey, out var previous);
            var fingerprint = source.SourceHash is null ? null : AssetArtifactFingerprint.Compute(
                kind,
                source.SourceHash,
                previous?.Dependencies.Select(dependency => (
                    dependency.Kind,
                    dependency.DependencyKey,
                    dependency.ArtifactFingerprint ?? "missing")) ?? []);
            var reused = !current.Force && source.Error is null && previous is
                { IsStale: false, ArtifactFingerprint: not null } &&
                previous.SourceHash == source.SourceHash && previous.ArtifactFingerprint == fingerprint;
            var item = new AssetImportWorkItem
            {
                Id = Guid.NewGuid(),
                GameVersion = current.GameVersion,
                RunId = current.Id,
                ImportKind = kind,
                SourceKey = source.SourceKey,
                NormalizedSourceKey = normalizedSourceKey,
                SourcePath = source.SourcePath,
                SourceHash = source.SourceHash,
                ArtifactFingerprint = reused ? fingerprint : null,
                Status = reused ? AssetImportJobValues.Reused : source.Error is null ? AssetImportJobValues.Queued : AssetImportJobValues.Failed,
                CreatedAt = now,
                FinishedAt = reused || source.Error is not null ? now : null,
                Error = source.Error
            };
            current.WorkItems.Add(item);
            if (reused)
            {
                await outbox.PublishAsync(new AssetImportWorkItemCompleted(current.Id, item.Id));
            }
            else if (source.Error is null)
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
        current.LastHeartbeatAt = now;
        current.Status = AssetImportJobValues.Running;
        if (sources.Count == 0) await outbox.PublishAsync(new FinalizeAssetImportRun(current.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(MultiFlushMode.AllowMultiples, cancellationToken);
    }

    private async Task<IReadOnlyList<DiscoveredSource>> DiscoverFileSourcesAsync(
        string gameVersion,
        string kind,
        CancellationToken cancellationToken)
    {
        var root = VersionRoot(gameVersion);
        var paths = AssetImportFileDiscovery.Paths(root, kind);
        var result = new List<DiscoveredSource>(paths.Count);
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (HasSymbolicLink(root, RelativeSourceKey(root, path)))
                    throw new InvalidDataException("Symbolic-link sources are not supported.");
                var hash = await AssetImportSourceHash.FileAsync(path, cancellationToken);
                result.Add(new DiscoveredSource(RelativeSourceKey(root, path), Path.GetFullPath(path), hash, null));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                result.Add(new DiscoveredSource(RelativeSourceKey(root, path), Path.GetFullPath(path), null, exception.Message));
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
                source.Catalog.Kind == AssetImportJobValues.Maps && source.Catalog.IsActive)
            .OrderBy(source => source.SourceKey)
            .Select(source => new { source.SourceKey, source.SourceHash })
            .ToListAsync(cancellationToken);
        var root = VersionRoot(gameVersion);
        return sources.Select(source => new DiscoveredSource(
            source.SourceKey,
            Path.Combine(root, source.SourceKey),
            MapPreviewGeneration.ComputeSourceHash(source.SourceHash),
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

    private string VersionRoot(string gameVersion) =>
        AssetImportSourcePaths.VersionRoot(options.Value.SourceRootPath, gameVersion);

    private static object FileCommand(string kind, Guid id) => kind switch
    {
        AssetImportJobValues.Textures => new ImportTextureFile(id),
        AssetImportJobValues.StaticMeshes => new ImportStaticMeshFile(id),
        AssetImportJobValues.Sounds => new ImportSoundFile(id),
        AssetImportJobValues.Music => new ImportMusicFile(id),
        AssetImportJobValues.Maps => new ImportMapFile(id),
        AssetImportJobValues.Scenes => new ImportSceneFile(id),
        AssetImportJobValues.MapPreviews => new GenerateMapPreview(id),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string Truncate(string value) => value.Length <= 4000 ? value : value[..4000];
    private static bool IsDiscoveryFailure(Exception exception) =>
        exception is not OperationCanceledException &&
        exception is not DbException &&
        exception is not DbUpdateException &&
        exception.InnerException is not DbException;
    private static string NormalizeSourceKey(string value) => value.Trim().ToLowerInvariant();
    private static string RelativeSourceKey(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');
    private static bool HasSymbolicLink(string root, string relativePath)
    {
        var current = Path.GetFullPath(root);
        foreach (var segment in relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current))
            {
                if (new FileInfo(current).LinkTarget is not null) return true;
            }
            else if (Directory.Exists(current) && new DirectoryInfo(current).LinkTarget is not null)
            {
                return true;
            }
        }
        return false;
    }
    private sealed record DiscoveredSource(string SourceKey, string SourcePath, string? SourceHash, string? Error);
}

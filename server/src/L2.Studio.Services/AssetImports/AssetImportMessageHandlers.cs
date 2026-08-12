using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Services;

public sealed class AssetStorageReconciliationPublisher(IWolverineRuntime runtime) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) =>
        await new MessageBus(runtime).PublishAsync(new ReconcileAssetStorage());

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

[WolverineHandler]
public sealed class AssetImportFileHandlers(IAssetImportWorkItemProcessor processor)
{
    public Task Handle(ImportTextureFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportStaticMeshFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportSoundFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportMusicFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportMapFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportSceneFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(GenerateMapPreview message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
}

[WolverineHandler]
public sealed class AssetImportRunHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    TimeProvider timeProvider)
{
    public async Task Handle(AssetImportWorkItemCompleted message, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var run = await context.AssetImportRuns.Include(item => item.WorkItems)
            .SingleOrDefaultAsync(item => item.Id == message.RunId, cancellationToken);
        if (run is null || AssetImportJobValues.TerminalStatuses.Contains(run.Status))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }
        ApplyCounts(run);
        outbox.Enroll(context);
        if (run.DiscoveryFinishedAt is not null && run.CompletedFileCount == run.DiscoveredFileCount)
            await outbox.PublishAsync(new FinalizeAssetImportRun(run.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(MultiFlushMode.AllowMultiples, cancellationToken);
    }

    public async Task Handle(FinalizeAssetImportRun message, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var run = await context.AssetImportRuns.Include(item => item.WorkItems)
            .SingleOrDefaultAsync(item => item.Id == message.RunId, cancellationToken);
        if (run is null || AssetImportJobValues.TerminalStatuses.Contains(run.Status))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }
        ApplyCounts(run);
        if (run.DiscoveryFinishedAt is null || run.CompletedFileCount != run.DiscoveredFileCount)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        outbox.Enroll(context);
        if (run.TriggerType == AssetImportJobValues.FullScan)
        {
            var discovered = run.WorkItems.Select(item => item.NormalizedSourceKey).ToHashSet(StringComparer.Ordinal);
            var catalog = await context.AssetCatalogs.Include(item => item.Sources)
                .SingleOrDefaultAsync(item => item.GameVersion == run.GameVersion &&
                    item.Kind == run.Kind && item.IsActive, cancellationToken);
            if (catalog is not null)
            {
                var removed = catalog.Sources.Where(source => !discovered.Contains(source.NormalizedSourceKey)).ToArray();
                foreach (var source in removed)
                {
                    await MarkRemovedDependencyStaleAsync(
                        context, run.GameVersion, run.Kind, source.NormalizedSourceKey,
                        source.SourceKey, timeProvider.GetUtcNow(), cancellationToken);
                    context.AssetCatalogSources.Remove(source);
                }
                var remaining = catalog.Sources.Where(source => !removed.Contains(source)).ToArray();
                catalog.SourceHash = AggregateHash(remaining.Select(source =>
                    (source.NormalizedSourceKey, source.SourceHash)));
                catalog.MetadataJson = AssetCatalogMetadataAggregator.Aggregate(
                    run.Kind, remaining.Select(source => source.MetadataJson));
                catalog.PublishedAt = timeProvider.GetUtcNow();
            }
        }
        run.Status = run.FailedFileCount > 0
            ? AssetImportJobValues.Failed
            : run.WarningFileCount > 0
                ? AssetImportJobValues.SucceededWithWarnings
                : AssetImportJobValues.Succeeded;
        run.FinishedAt = timeProvider.GetUtcNow();
        await outbox.SaveChangesAndFlushMessagesAsync(MultiFlushMode.AllowMultiples, cancellationToken);
    }

    internal static void ApplyCounts(AssetImportRun run)
    {
        run.CompletedFileCount = run.WorkItems.Count(item => AssetImportJobValues.WorkItemTerminalStatuses.Contains(item.Status));
        run.SucceededFileCount = run.WorkItems.Count(item =>
            item.Status is AssetImportJobValues.Succeeded or AssetImportJobValues.SucceededWithWarnings);
        run.WarningFileCount = run.WorkItems.Count(item => item.WarningCount > 0);
        run.FailedFileCount = run.WorkItems.Count(item => item.Status == AssetImportJobValues.Failed);
        run.ReusedFileCount = run.WorkItems.Count(item => item.Status == AssetImportJobValues.Reused);
    }

    private static string AggregateHash(IEnumerable<(string SourceKey, string SourceHash)> sources)
    {
        var value = string.Join('\n', sources.OrderBy(item => item.SourceKey, StringComparer.Ordinal)
            .Select(item => $"{item.SourceKey}\0{item.SourceHash}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static async Task MarkRemovedDependencyStaleAsync(
        GameContentDbContext context,
        string gameVersion,
        string kind,
        string normalizedSourceKey,
        string sourceKey,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var dependencies = await context.AssetCatalogSourceDependencies.Include(dependency => dependency.Source)
            .ThenInclude(source => source.Catalog)
            .Where(dependency => dependency.Kind == kind &&
                dependency.ResolvedSourceKey == normalizedSourceKey &&
                dependency.Source.Catalog.GameVersion == gameVersion && dependency.Source.Catalog.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var dependency in dependencies)
        {
            dependency.Source.IsStale = true;
            dependency.Source.StaleAt ??= now;
            var reasons = JsonSerializer.Deserialize<List<string>>(dependency.Source.StaleReasonsJson) ?? [];
            var reason = $"Dependency {kind}:{sourceKey} was removed.";
            if (!reasons.Contains(reason, StringComparer.Ordinal)) reasons.Add(reason);
            dependency.Source.StaleReasonsJson = JsonSerializer.Serialize(reasons);
        }
    }
}

[WolverineHandler]
public sealed class AssetStorageHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider)
{
    public async Task Handle(DeleteAssetVersion message, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(options.Value.AssetRootPath);
        var path = ContainedPath(root, message.RelativePath);
        if (!Directory.Exists(path) || !File.Exists(Path.Combine(path, ".l2-asset-version"))) return;
        if (!message.Force && await IsReferencedAsync(message.RelativePath, cancellationToken)) return;
        Directory.Delete(path, recursive: true);
    }

    public async Task Handle(ReconcileAssetStorage _, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(options.Value.AssetRootPath);
        Directory.CreateDirectory(root);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var running = await context.AssetImportWorkItems.AsNoTracking()
            .Where(item => item.Status == AssetImportJobValues.Running)
            .Select(item => item.Id).ToListAsync(cancellationToken);
        CleanStagingRoot(Path.GetFullPath(options.Value.AssetWorkRootPath), running);
        CleanStagingRoot(Path.GetFullPath(options.Value.SourceSnapshotRootPath), running, ".locks");

        var affectedCatalogs = new HashSet<AssetCatalog>();
        var sources = await context.AssetCatalogSources.Include(source => source.Catalog)
            .Include(source => source.Artifact).ToListAsync(cancellationToken);
        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outputPath = ContainedPath(root, source.OutputRoot);
            var marker = Path.Combine(outputPath, ".l2-asset-version");
            if (Directory.Exists(outputPath) && File.Exists(marker) &&
                string.Equals((await File.ReadAllTextAsync(marker, cancellationToken)).Trim(),
                    source.Artifact.BuildFingerprint, StringComparison.Ordinal))
            {
                source.Artifact.IntegrityStatus = "healthy";
                source.Artifact.LastVerifiedAt = timeProvider.GetUtcNow();
                continue;
            }
            source.Artifact.IntegrityStatus = Directory.Exists(outputPath) ? "corrupt" : "missing";
            source.Artifact.LastVerifiedAt = timeProvider.GetUtcNow();
            var runId = await context.AssetImportWorkItems.Where(item => item.Id == source.PublishingWorkItemId)
                .Select(item => (Guid?)item.RunId).SingleOrDefaultAsync(cancellationToken);
            if (runId is not null)
            {
                context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
                {
                    RunId = runId.Value,
                    WorkItemId = source.PublishingWorkItemId,
                    Severity = "error",
                    Code = "reconcile.output_missing",
                    Stage = "reconcile",
                    SourceKey = source.SourceKey,
                    Message = "The catalog source was unpublished because its immutable output directory is missing.",
                    CreatedAt = timeProvider.GetUtcNow()
                });
            }
            await MarkMissingDependencyStaleAsync(context, source, timeProvider.GetUtcNow(), cancellationToken);
            affectedCatalogs.Add(source.Catalog);
            context.AssetCatalogSources.Remove(source);
        }
        foreach (var catalog in affectedCatalogs)
        {
            var remaining = catalog.Sources.Where(source =>
                context.Entry(source).State != EntityState.Deleted).ToArray();
            catalog.SourceHash = AggregateHash(remaining.Select(source =>
                (source.NormalizedSourceKey, source.SourceHash)));
            catalog.MetadataJson = AssetCatalogMetadataAggregator.Aggregate(
                catalog.Kind, remaining.Select(source => source.MetadataJson));
            catalog.PublishedAt = timeProvider.GetUtcNow();
        }
        await context.SaveChangesAsync(cancellationToken);

    }

    private async Task<bool> IsReferencedAsync(string relativePath, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await context.AssetArtifacts.AsNoTracking().AnyAsync(
                artifact => artifact.OutputRoot == relativePath, cancellationToken))
            return true;
        if (await context.AssetCatalogSources.AsNoTracking().AnyAsync(source => source.OutputRoot == relativePath, cancellationToken))
            return true;
        var json = await context.AssetCatalogSources.AsNoTracking()
            .Select(source => source.ReferencedOutputRootsJson).ToListAsync(cancellationToken);
        return json.SelectMany(value => JsonSerializer.Deserialize<string[]>(value) ?? [])
            .Contains(relativePath, StringComparer.Ordinal);
    }

    private static async Task MarkMissingDependencyStaleAsync(
        GameContentDbContext context,
        AssetCatalogSource missing,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var dependencies = await context.AssetCatalogSourceDependencies.Include(dependency => dependency.Source)
            .ThenInclude(source => source.Catalog)
            .Where(dependency => dependency.Kind == missing.Catalog.Kind &&
                dependency.ResolvedSourceKey == missing.NormalizedSourceKey &&
                dependency.Source.Catalog.GameVersion == missing.Catalog.GameVersion &&
                dependency.Source.Catalog.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var dependency in dependencies)
        {
            dependency.Source.IsStale = true;
            dependency.Source.StaleAt ??= now;
            var reasons = JsonSerializer.Deserialize<List<string>>(dependency.Source.StaleReasonsJson) ?? [];
            var reason = $"Dependency {missing.Catalog.Kind}:{missing.SourceKey} is unavailable.";
            if (!reasons.Contains(reason, StringComparer.Ordinal)) reasons.Add(reason);
            dependency.Source.StaleReasonsJson = JsonSerializer.Serialize(reasons);
        }
    }

    private static void CleanStagingRoot(
        string stagingRoot,
        IReadOnlyCollection<Guid> running,
        string? ignoredDirectory = null)
    {
        if (!Directory.Exists(stagingRoot)) return;
        foreach (var path in Directory.EnumerateDirectories(stagingRoot))
        {
            if (Path.GetFileName(path) == ignoredDirectory) continue;
            if (!Guid.TryParseExact(Path.GetFileName(path), "N", out var id) || !running.Contains(id))
                Directory.Delete(path, recursive: true);
        }
    }

    private static string AggregateHash(IEnumerable<(string SourceKey, string SourceHash)> sources)
    {
        var value = string.Join('\n', sources.OrderBy(item => item.SourceKey, StringComparer.Ordinal)
            .Select(item => $"{item.SourceKey}\0{item.SourceHash}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string ContainedPath(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) throw new InvalidDataException("Asset version paths must be relative.");
        var path = Path.GetFullPath(Path.Combine(root, relativePath));
        var relative = Path.GetRelativePath(root, path);
        if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal))
            throw new InvalidDataException("Asset version path escaped the configured asset root.");
        return path;
    }
}

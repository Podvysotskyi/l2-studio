using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class AssetImportRunHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
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
        if (IsReadyToFinalize(run))
            await FinalizeAsync(context, run, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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
        if (!IsReadyToFinalize(run))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await FinalizeAsync(context, run, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    internal static bool IsReadyToFinalize(AssetImportRun run) =>
        !AssetImportJobValues.TerminalStatuses.Contains(run.Status) &&
        run.DiscoveryFinishedAt is not null &&
        run.CompletedFileCount == run.DiscoveredFileCount;

    private async Task FinalizeAsync(
        GameContentDbContext context,
        AssetImportRun run,
        CancellationToken cancellationToken)
    {
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

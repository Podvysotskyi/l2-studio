using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Contracts.Responses;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Repositories;

public sealed class ImportJobRepository(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    TimeProvider timeProvider) : IImportJobRepository
{
    public async Task<ImportJobSummary?> QueueContentAsync(
        string gameVersion,
        string target,
        string mode,
        CancellationToken cancellationToken)
    {
        var family = ContentImportTargetValues.Family(target);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({$"l2-content-import:{gameVersion}:{family}"}))",
            cancellationToken);
        if (await context.ContentImportRuns.AnyAsync(run =>
                run.GameVersion == gameVersion && run.ConcurrencyKey == family &&
                ImportJobValues.ActiveStatuses.Contains(run.Status), cancellationToken))
            return null;

        var run = new ContentImportRun
        {
            Id = Guid.NewGuid(),
            GameVersion = gameVersion,
            Kind = target,
            ConcurrencyKey = family,
            Mode = mode,
            Status = ImportJobValues.Queued,
            RequestedAt = timeProvider.GetUtcNow()
        };
        context.ContentImportRuns.Add(run);
        outbox.Enroll(context);
        await outbox.PublishAsync(new RunContentImport(run.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return ToSummary(run);
    }

    public async Task<ImportJobPage> GetPageAsync(
        string gameVersion,
        string? category,
        string? target,
        string? status,
        string? searchQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.ImportJobs.AsNoTracking().Where(job => job.GameVersion == gameVersion);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(job => job.Category == category);
        if (!string.IsNullOrWhiteSpace(target)) query = query.Where(job => job.Kind == target);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(job => job.Status == status);
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim();
            query = query.Where(job =>
                EF.Functions.ILike(job.Kind, $"%{EscapeLike(search)}%", "\\") ||
                job.Error != null && EF.Functions.ILike(job.Error, $"%{EscapeLike(search)}%", "\\"));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var jobs = await query.OrderByDescending(job => job.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new ImportJobPage(jobs.Select(ToSummary).ToArray(), total, page, pageSize);
    }

    public async Task<ImportJobSummary?> GetAsync(
        string gameVersion,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var job = await context.ImportJobs.AsNoTracking().SingleOrDefaultAsync(
            value => value.Id == id && value.GameVersion == gameVersion, cancellationToken);
        return job is null ? null : ToSummary(job);
    }

    public static ImportJobSummary ToSummary(ImportJob job) => job switch
    {
        ContentImportRun content => new ImportJobSummary(
            content.Id, content.Category, content.Kind, content.Mode, content.Status, null, false,
            content.RequestedAt, content.StartedAt, null, content.FinishedAt,
            content.TotalCount, ContentCompletedCount(content),
            [
                new ImportJobMetricSummary("inserted", content.InsertedCount),
                new ImportJobMetricSummary("existing", content.ExistingCount),
                new ImportJobMetricSummary("restored", content.RestoredCount)
            ], content.Error),
        AssetImportRun asset => new ImportJobSummary(
            asset.Id, asset.Category, asset.Kind, asset.TriggerType, asset.Status,
            asset.RequestedSourceKey, asset.Force, asset.RequestedAt, asset.StartedAt,
            asset.DiscoveryFinishedAt, asset.FinishedAt, asset.DiscoveredFileCount,
            asset.CompletedFileCount,
            [
                new ImportJobMetricSummary("succeeded", asset.SucceededFileCount),
                new ImportJobMetricSummary("warnings", asset.WarningFileCount),
                new ImportJobMetricSummary("failed", asset.FailedFileCount),
                new ImportJobMetricSummary("reused", asset.ReusedFileCount)
            ], asset.Error),
        _ => throw new ArgumentOutOfRangeException(nameof(job))
    };

    private static int ContentCompletedCount(ContentImportRun run) =>
        ImportJobValues.TerminalStatuses.Contains(run.Status)
            ? run.TotalCount
            : Math.Min(run.TotalCount, run.InsertedCount + run.ExistingCount);

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
}

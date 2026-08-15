using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace L2.Studio.Services;

public sealed class ImportJobAbandonmentService(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider,
    ILogger<ImportJobAbandonmentService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await FailAbandonedRunsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to check for abandoned import jobs");
            }
        }
    }

    internal async Task<int> FailAbandonedRunsAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var cutoff = now - options.Value.AbandonedRunTimeout;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var jobs = await context.ImportJobs.Where(job =>
                (job.Status == ImportJobValues.Discovering || job.Status == ImportJobValues.Running) &&
                (job.LastHeartbeatAt ?? job.StartedAt ?? job.RequestedAt) < cutoff)
            .ToListAsync(cancellationToken);
        foreach (var job in jobs)
        {
            var message = $"Import stopped because no heartbeat was recorded for {options.Value.AbandonedRunTimeout.TotalMinutes:0} minutes.";
            job.Status = ImportJobValues.Failed;
            job.Error = message;
            job.FinishedAt = now;
            if (job is not AssetImportRun) continue;

            var items = await context.AssetImportWorkItems.Where(item => item.RunId == job.Id &&
                    !AssetImportJobValues.WorkItemTerminalStatuses.Contains(item.Status))
                .ToListAsync(cancellationToken);
            foreach (var item in items)
            {
                item.Status = AssetImportJobValues.Failed;
                item.Error = message;
                item.FinishedAt = now;
                context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
                {
                    RunId = job.Id, WorkItemId = item.Id, Severity = "error",
                    Code = "execution.abandoned", Stage = "execution", SourceKey = item.SourceKey,
                    Message = message, CreatedAt = now
                });
            }
            context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
            {
                RunId = job.Id, Severity = "error", Code = "execution.abandoned",
                Stage = "execution", Message = message, CreatedAt = now
            });
        }
        if (jobs.Count > 0) await context.SaveChangesAsync(cancellationToken);
        return jobs.Count;
    }
}

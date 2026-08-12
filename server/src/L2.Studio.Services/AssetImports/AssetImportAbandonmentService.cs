using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace L2.Studio.Services;

public sealed class AssetImportAbandonmentService(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider,
    ILogger<AssetImportAbandonmentService> logger) : BackgroundService
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
                logger.LogError(exception, "Failed to check for abandoned asset import runs");
            }
        }
    }

    internal async Task<int> FailAbandonedRunsAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var cutoff = now - options.Value.AbandonedRunTimeout;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var runs = await context.AssetImportRuns.Include(run => run.WorkItems)
            .Where(run => (run.Status == AssetImportJobValues.Discovering || run.Status == AssetImportJobValues.Running) &&
                (run.LastHeartbeatAt ?? run.StartedAt ?? run.RequestedAt) < cutoff)
            .ToListAsync(cancellationToken);
        foreach (var run in runs)
        {
            var message = $"Import stopped because no heartbeat was recorded for {options.Value.AbandonedRunTimeout.TotalMinutes:0} minutes.";
            run.Status = AssetImportJobValues.Failed;
            run.Error = message;
            run.FinishedAt = now;
            foreach (var item in run.WorkItems.Where(item =>
                !AssetImportJobValues.WorkItemTerminalStatuses.Contains(item.Status)))
            {
                item.Status = AssetImportJobValues.Failed;
                item.Error = message;
                item.FinishedAt = now;
                context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
                {
                    RunId = run.Id,
                    WorkItemId = item.Id,
                    Severity = "error",
                    Code = "execution.abandoned",
                    Stage = "execution",
                    SourceKey = item.SourceKey,
                    Message = message,
                    CreatedAt = now
                });
            }
            context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
            {
                RunId = run.Id,
                Severity = "error",
                Code = "execution.abandoned",
                Stage = "execution",
                Message = message,
                CreatedAt = now
            });
        }
        if (runs.Count > 0) await context.SaveChangesAsync(cancellationToken);
        return runs.Count;
    }
}

using L2.Studio.Context;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Services;

internal sealed class AssetImportHeartbeatLease : IAsyncDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private readonly CancellationTokenSource cancellation;
    private readonly Task loop;

    private AssetImportHeartbeatLease(
        IDbContextFactory<GameContentDbContext> contextFactory,
        TimeProvider timeProvider,
        Guid runId,
        Guid? workItemId,
        CancellationToken cancellationToken)
    {
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        loop = RunAsync(contextFactory, timeProvider, runId, workItemId, cancellation.Token);
    }

    public static AssetImportHeartbeatLease Start(
        IDbContextFactory<GameContentDbContext> contextFactory,
        TimeProvider timeProvider,
        Guid runId,
        Guid? workItemId,
        CancellationToken cancellationToken) =>
        new(contextFactory, timeProvider, runId, workItemId, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await cancellation.CancelAsync();
        try
        {
            await loop;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        cancellation.Dispose();
    }

    private static async Task RunAsync(
        IDbContextFactory<GameContentDbContext> contextFactory,
        TimeProvider timeProvider,
        Guid runId,
        Guid? workItemId,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var now = timeProvider.GetUtcNow();
            await context.AssetImportRuns.Where(run => run.Id == runId)
                .ExecuteUpdateAsync(update => update.SetProperty(run => run.LastHeartbeatAt, now), cancellationToken);
            if (workItemId is not null)
            {
                await context.AssetImportWorkItems.Where(item => item.Id == workItemId.Value)
                    .ExecuteUpdateAsync(update => update.SetProperty(item => item.LastHeartbeatAt, now), cancellationToken);
            }
        }
    }
}

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

public sealed class PlayerImportRepository(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    TimeProvider timeProvider) : IPlayerImportRepository
{
    public async Task<PlayerImportRunSummary?> QueueAsync(string gameVersion, string mode, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({$"l2-player-import:{gameVersion}"}))", cancellationToken);
        if (await context.PlayerImportRuns.AnyAsync(run =>
                run.GameVersion == gameVersion && PlayerImportJobValues.ActiveStatuses.Contains(run.Status), cancellationToken))
            return null;

        var run = new PlayerImportRun
        {
            Id = Guid.NewGuid(), GameVersion = gameVersion, Mode = mode,
            Status = PlayerImportJobValues.Queued, RequestedAt = timeProvider.GetUtcNow()
        };
        context.PlayerImportRuns.Add(run);
        outbox.Enroll(context);
        await outbox.PublishAsync(new ImportC1Players(run.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return Summary(run);
    }

    public async Task<IReadOnlyList<PlayerImportRunSummary>> GetRecentAsync(
        string gameVersion, int limit, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await context.PlayerImportRuns.AsNoTracking().Where(run => run.GameVersion == gameVersion)
                .OrderByDescending(run => run.RequestedAt).Take(limit).ToListAsync(cancellationToken))
            .Select(Summary).ToArray();
    }

    public async Task<PlayerImportRunSummary?> GetAsync(string gameVersion, Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = await context.PlayerImportRuns.AsNoTracking().SingleOrDefaultAsync(
            value => value.Id == id && value.GameVersion == gameVersion, cancellationToken);
        return run is null ? null : Summary(run);
    }

    private static PlayerImportRunSummary Summary(PlayerImportRun run) => new(
        run.Id, run.Mode, run.Status, run.RequestedAt, run.StartedAt, run.FinishedAt,
        run.TotalCount, run.InsertedCount, run.ExistingCount, run.RestoredCount, run.Error);
}

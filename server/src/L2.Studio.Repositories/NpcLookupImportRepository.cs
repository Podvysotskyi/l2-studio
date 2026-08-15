using L2.Studio.Contracts;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Repositories;

public sealed class NpcLookupImportRepository(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    TimeProvider timeProvider) : INpcLookupImportRepository
{
    public async Task<NpcLookupImportRunSummary?> QueueAsync(
        string gameVersion,
        string kind,
        string mode,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({$"l2-npc-lookup-import:{gameVersion}:{kind}"}))",
            cancellationToken);
        if (await context.NpcLookupImportRuns.AnyAsync(run =>
                run.GameVersion == gameVersion && run.Kind == kind &&
                NpcLookupImportJobValues.ActiveStatuses.Contains(run.Status), cancellationToken))
        {
            return null;
        }

        var run = new NpcLookupImportRun
        {
            Id = Guid.NewGuid(),
            GameVersion = gameVersion,
            Kind = kind,
            Mode = mode,
            Status = NpcLookupImportJobValues.Queued,
            RequestedAt = timeProvider.GetUtcNow()
        };
        context.NpcLookupImportRuns.Add(run);
        outbox.Enroll(context);
        await outbox.PublishAsync(Command(gameVersion, kind, run.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return ToSummary(run);
    }

    public async Task<IReadOnlyList<NpcLookupImportRunSummary>> GetRecentAsync(
        string gameVersion,
        string kind,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await context.NpcLookupImportRuns.AsNoTracking()
                .Where(run => run.GameVersion == gameVersion && run.Kind == kind)
                .OrderByDescending(run => run.RequestedAt)
                .Take(limit)
                .ToListAsync(cancellationToken))
            .Select(ToSummary)
            .ToArray();
    }

    public async Task<NpcLookupImportRunSummary?> GetAsync(
        string gameVersion,
        string kind,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = await context.NpcLookupImportRuns.AsNoTracking().SingleOrDefaultAsync(
            value => value.Id == id && value.GameVersion == gameVersion && value.Kind == kind,
            cancellationToken);
        return run is null ? null : ToSummary(run);
    }

    private static object Command(string gameVersion, string kind, Guid runId) => (gameVersion, kind) switch
    {
        ("c1", NpcLookupImportJobValues.Npcs) => new ImportC1Npcs(runId),
        ("c1", NpcLookupImportJobValues.NpcTypes) => new ImportC1NpcTypes(runId),
        ("c4", NpcLookupImportJobValues.NpcTypes) => new ImportC4NpcTypes(runId),
        ("interlude", NpcLookupImportJobValues.NpcTypes) => new ImportInterludeNpcTypes(runId),
        ("c1", NpcLookupImportJobValues.NpcRaces) => new ImportC1NpcRaces(runId),
        ("c4", NpcLookupImportJobValues.NpcRaces) => new ImportC4NpcRaces(runId),
        ("interlude", NpcLookupImportJobValues.NpcRaces) => new ImportInterludeNpcRaces(runId),
        ("c1", NpcLookupImportJobValues.NpcSexes) => new ImportC1NpcSexes(runId),
        ("c4", NpcLookupImportJobValues.NpcSexes) => new ImportC4NpcSexes(runId),
        ("interlude", NpcLookupImportJobValues.NpcSexes) => new ImportInterludeNpcSexes(runId),
        _ => throw new ArgumentOutOfRangeException(nameof(gameVersion))
    };

    private static NpcLookupImportRunSummary ToSummary(NpcLookupImportRun run) => new(
        run.Id,
        run.Kind,
        run.Mode,
        run.Status,
        run.RequestedAt,
        run.StartedAt,
        run.FinishedAt,
        run.TotalCount,
        run.InsertedCount,
        run.ExistingCount,
        run.RestoredCount,
        run.Error);
}

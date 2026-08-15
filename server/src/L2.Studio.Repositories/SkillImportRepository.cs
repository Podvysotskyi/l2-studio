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

public sealed class SkillImportRepository(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    TimeProvider timeProvider) : ISkillImportRepository
{
    public async Task<SkillImportRunSummary?> QueueAsync(string gameVersion, string mode, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({$"l2-skill-import:{gameVersion}"}))", cancellationToken);
        if (await context.SkillImportRuns.AnyAsync(run =>
                run.GameVersion == gameVersion && SkillImportJobValues.ActiveStatuses.Contains(run.Status), cancellationToken))
            return null;

        var run = new SkillImportRun
        {
            Id = Guid.NewGuid(), GameVersion = gameVersion, Mode = mode,
            Status = SkillImportJobValues.Queued, RequestedAt = timeProvider.GetUtcNow()
        };
        context.SkillImportRuns.Add(run);
        outbox.Enroll(context);
        await outbox.PublishAsync(new ImportC1Skills(run.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return Summary(run);
    }

    public async Task<IReadOnlyList<SkillImportRunSummary>> GetRecentAsync(
        string gameVersion, int limit, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await context.SkillImportRuns.AsNoTracking().Where(run => run.GameVersion == gameVersion)
                .OrderByDescending(run => run.RequestedAt).Take(limit).ToListAsync(cancellationToken))
            .Select(Summary).ToArray();
    }

    public async Task<SkillImportRunSummary?> GetAsync(string gameVersion, Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = await context.SkillImportRuns.AsNoTracking().SingleOrDefaultAsync(
            value => value.Id == id && value.GameVersion == gameVersion, cancellationToken);
        return run is null ? null : Summary(run);
    }

    private static SkillImportRunSummary Summary(SkillImportRun run) => new(
        run.Id, run.Mode, run.Status, run.RequestedAt, run.StartedAt, run.FinishedAt,
        run.TotalCount, run.InsertedCount, run.ExistingCount, run.RestoredCount, run.Error);
}

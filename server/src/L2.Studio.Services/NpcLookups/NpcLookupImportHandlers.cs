using L2.Studio.Messages;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Services;

[WolverineHandler]
public sealed class NpcLookupImportHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public Task Handle(ImportC1NpcTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcTypes, NpcLookupCatalogs.C1Types, token);

    public Task Handle(ImportC4NpcTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcTypes, NpcLookupCatalogs.C4Types, token);

    public Task Handle(ImportInterludeNpcTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcTypes, NpcLookupCatalogs.InterludeTypes, token);

    public Task Handle(ImportC1NpcRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcRaces, NpcLookupCatalogs.C1Races, token);

    public Task Handle(ImportC4NpcRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcRaces, NpcLookupCatalogs.C4Races, token);

    public Task Handle(ImportInterludeNpcRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcRaces, NpcLookupCatalogs.InterludeRaces, token);

    private async Task ImportAsync(
        Guid runId,
        string kind,
        IReadOnlyList<NpcLookupDefinition> definitions,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var run = await context.NpcLookupImportRuns.SingleOrDefaultAsync(
                value => value.Id == runId && value.Kind == kind, cancellationToken);
            if (run is null || NpcLookupImportJobValues.TerminalStatuses.Contains(run.Status)) return;

            var now = timeProvider.GetUtcNow();
            run.Status = NpcLookupImportJobValues.Running;
            run.StartedAt ??= now;
            var existingNames = kind == NpcLookupImportJobValues.NpcTypes
                ? await context.NpcTypes.AsNoTracking()
                    .Where(item => item.GameVersion == run.GameVersion)
                    .Select(item => item.Name)
                    .ToHashSetAsync(StringComparer.Ordinal, cancellationToken)
                : await context.NpcRaces.AsNoTracking()
                    .Where(item => item.GameVersion == run.GameVersion)
                    .Select(item => item.Name)
                    .ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
            var missing = definitions.Where(definition => !existingNames.Contains(definition.Name)).ToArray();
            if (kind == NpcLookupImportJobValues.NpcTypes)
            {
                context.NpcTypes.AddRange(missing.Select(definition => new NpcType
                {
                    GameVersion = run.GameVersion,
                    Name = definition.Name,
                    DisplayName = definition.DisplayName
                }));
            }
            else
            {
                context.NpcRaces.AddRange(missing.Select(definition => new NpcRace
                {
                    GameVersion = run.GameVersion,
                    Name = definition.Name,
                    DisplayName = definition.DisplayName
                }));
            }

            run.TotalCount = definitions.Count;
            run.InsertedCount = missing.Length;
            run.ExistingCount = definitions.Count - missing.Length;
            run.Status = NpcLookupImportJobValues.Succeeded;
            run.FinishedAt = timeProvider.GetUtcNow();
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await MarkFailedAsync(runId, exception, cancellationToken);
        }
    }

    private async Task MarkFailedAsync(Guid runId, Exception exception, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = await context.NpcLookupImportRuns.SingleOrDefaultAsync(value => value.Id == runId, cancellationToken);
        if (run is null || NpcLookupImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = NpcLookupImportJobValues.Failed;
        run.FinishedAt = timeProvider.GetUtcNow();
        run.Error = exception.Message.Length <= 4000 ? exception.Message : exception.Message[..4000];
        await context.SaveChangesAsync(cancellationToken);
    }
}

using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class NpcLookupImportHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    TimeProvider timeProvider)
{
    private static readonly NpcLookupCatalog C1Catalog = new C1NpcLookupCatalog();
    private static readonly NpcLookupCatalog C4Catalog = new C4NpcLookupCatalog();
    private static readonly NpcLookupCatalog InterludeCatalog = new InterludeNpcLookupCatalog();

    public Task Handle(ImportC1NpcTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcTypes, C1Catalog.Types, token);

    public Task Handle(ImportC4NpcTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcTypes, C4Catalog.Types, token);

    public Task Handle(ImportInterludeNpcTypes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcTypes, InterludeCatalog.Types, token);

    public Task Handle(ImportC1NpcRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcRaces, C1Catalog.Races, token);

    public Task Handle(ImportC4NpcRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcRaces, C4Catalog.Races, token);

    public Task Handle(ImportInterludeNpcRaces message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcRaces, InterludeCatalog.Races, token);

    public Task Handle(ImportC1NpcSexes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcSexes, C1Catalog.Sexes, token);

    public Task Handle(ImportC4NpcSexes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcSexes, C4Catalog.Sexes, token);

    public Task Handle(ImportInterludeNpcSexes message, CancellationToken token) =>
        ImportAsync(message.RunId, NpcLookupImportJobValues.NpcSexes, InterludeCatalog.Sexes, token);

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
            var existingNames = await ExistingNamesAsync(context, kind, run.GameVersion, cancellationToken);
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
            else if (kind == NpcLookupImportJobValues.NpcRaces)
            {
                context.NpcRaces.AddRange(missing.Select(definition => new NpcRace
                {
                    GameVersion = run.GameVersion,
                    Name = definition.Name,
                    DisplayName = definition.DisplayName
                }));
            }
            else
            {
                context.NpcSexes.AddRange(missing.Select(definition => new NpcSex
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

    private static Task<HashSet<string>> ExistingNamesAsync(
        GameContentDbContext context,
        string kind,
        string gameVersion,
        CancellationToken cancellationToken) => kind switch
    {
        NpcLookupImportJobValues.NpcTypes => context.NpcTypes.AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .Select(item => item.Name)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken),
        NpcLookupImportJobValues.NpcRaces => context.NpcRaces.AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .Select(item => item.Name)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken),
        NpcLookupImportJobValues.NpcSexes => context.NpcSexes.AsNoTracking()
            .Where(item => item.GameVersion == gameVersion)
            .Select(item => item.Name)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

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

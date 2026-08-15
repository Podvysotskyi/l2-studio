using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class NpcImportHandlers(
    IDbContextFactory<GameContentDbContext> contextFactory,
    TimeProvider timeProvider)
{
    private static readonly C1NpcLookupCatalog C1Catalog = new();

    public Task Handle(ImportC1Npcs message, CancellationToken token) => ImportC1Async(message.RunId, token);

    private async Task ImportC1Async(Guid runId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var run = await context.NpcLookupImportRuns.SingleOrDefaultAsync(
                value => value.Id == runId && value.Kind == NpcLookupImportJobValues.Npcs,
                cancellationToken);
            if (run is null || NpcLookupImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            if (run.GameVersion != "c1" ||
                !NpcLookupImportJobValues.SupportedModes.Contains(run.Mode))
            {
                throw new InvalidOperationException("Only add-missing and restore-defaults C1 NPC imports are supported.");
            }

            var now = timeProvider.GetUtcNow();
            run.Status = NpcLookupImportJobValues.Running;
            run.StartedAt ??= now;
            await EnsureC1LookupsAsync(context, cancellationToken);

            var existing = await context.Npcs
                .Include(npc => npc.Status)
                .Include(npc => npc.Stats)
                .Include(npc => npc.StatsVitals)
                .Include(npc => npc.StatsAttack)
                .Include(npc => npc.StatsDefence)
                .Include(npc => npc.StatsSpeed)
                .Where(npc => npc.GameVersion == run.GameVersion)
                .ToDictionaryAsync(npc => npc.Id, cancellationToken);
            var existingDefinitions = existing.ToDictionary(
                    pair => pair.Key,
                    pair => new NpcDefinition(
                        pair.Value.Id,
                        pair.Value.AppearanceId ?? 0,
                        pair.Value.Level,
                        pair.Value.Name,
                        pair.Value.NpcTypeName,
                        pair.Value.NpcRaceName,
                        pair.Value.NpcSexName,
                        pair.Value.Status is null ? null : ToDefinition(pair.Value.Status),
                        pair.Value.Stats is null ? null : ToDefinition(pair.Value.Stats),
                        pair.Value.StatsVitals is null ? null : ToDefinition(pair.Value.StatsVitals),
                        pair.Value.StatsAttack is null ? null : ToDefinition(pair.Value.StatsAttack),
                        pair.Value.StatsDefence is null ? null : ToDefinition(pair.Value.StatsDefence),
                        pair.Value.StatsSpeed is null ? null : ToDefinition(pair.Value.StatsSpeed)));
            var reconciliation = Reconcile(
                C1Catalog.Npcs,
                existingDefinitions,
                run.Mode == NpcLookupImportJobValues.RestoreDefaults);
            context.Npcs.AddRange(reconciliation.Missing.Select(definition => new Npc
            {
                GameVersion = run.GameVersion,
                Id = definition.Id,
                AppearanceId = definition.AppearanceId,
                Level = definition.Level,
                Name = definition.Name,
                NpcTypeName = definition.TypeName,
                NpcRaceName = definition.RaceName,
                NpcSexName = definition.SexName,
                Status = definition.Status is null ? null : ToEntity(run.GameVersion, definition.Id, definition.Status),
                Stats = definition.Stats is null ? null : ToEntity(run.GameVersion, definition.Id, definition.Stats),
                StatsVitals = definition.StatsVitals is null ? null : ToEntity(run.GameVersion, definition.Id, definition.StatsVitals),
                StatsAttack = definition.StatsAttack is null ? null : ToEntity(run.GameVersion, definition.Id, definition.StatsAttack),
                StatsDefence = definition.StatsDefence is null ? null : ToEntity(run.GameVersion, definition.Id, definition.StatsDefence),
                StatsSpeed = definition.StatsSpeed is null ? null : ToEntity(run.GameVersion, definition.Id, definition.StatsSpeed)
            }));
            foreach (var definition in reconciliation.AppearanceMappings)
                existing[definition.Id].AppearanceId = definition.AppearanceId;
            foreach (var definition in reconciliation.Restored)
            {
                var npc = existing[definition.Id];
                npc.AppearanceId = definition.AppearanceId;
                npc.Level = definition.Level;
                npc.Name = definition.Name;
                npc.NpcTypeName = definition.TypeName;
                npc.NpcRaceName = definition.RaceName;
                npc.NpcSexName = definition.SexName;
                if (definition.Status is not null) ApplyStatus(context, npc, definition.Status);
                ApplyStats(context, npc, definition, removeMissing: true);
            }
            if (run.Mode == NpcLookupImportJobValues.AddMissing)
            {
                foreach (var definition in MissingStatuses(C1Catalog.Npcs, existingDefinitions))
                {
                    ApplyStatus(context, existing[definition.Id], definition.Status!);
                }
                foreach (var definition in C1Catalog.Npcs.Where(definition => existing.ContainsKey(definition.Id)))
                    ApplyStats(context, existing[definition.Id], definition, removeMissing: false);
            }

            run.TotalCount = C1Catalog.Npcs.Count;
            run.InsertedCount = reconciliation.Missing.Length;
            run.ExistingCount = C1Catalog.Npcs.Count - reconciliation.Missing.Length;
            run.RestoredCount = reconciliation.Restored.Length;
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

    private static async Task EnsureC1LookupsAsync(
        GameContentDbContext context,
        CancellationToken cancellationToken)
    {
        var types = await context.NpcTypes.Where(item => item.GameVersion == "c1")
            .Select(item => item.Name).ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
        var races = await context.NpcRaces.Where(item => item.GameVersion == "c1")
            .Select(item => item.Name).ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
        var sexes = await context.NpcSexes.Where(item => item.GameVersion == "c1")
            .Select(item => item.Name).ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
        var missing = MissingC1Lookups(types, races, sexes);
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Import NPC types, races, and sexes before NPC definitions. Missing: {string.Join("; ", missing)}.");
        }
    }

    internal static (NpcDefinition[] Missing, NpcDefinition[] Restored, NpcDefinition[] AppearanceMappings) Reconcile(
        IReadOnlyList<NpcDefinition> definitions,
        IReadOnlyDictionary<int, NpcDefinition> existing,
        bool restoreDefaults)
    {
        var missing = definitions.Where(definition => !existing.ContainsKey(definition.Id)).ToArray();
        var restored = restoreDefaults
            ? definitions.Where(definition => existing.TryGetValue(definition.Id, out var value) && value != definition).ToArray()
            : [];
        var appearanceMappings = definitions.Where(definition =>
            existing.TryGetValue(definition.Id, out var value) && value.AppearanceId != definition.AppearanceId).ToArray();
        return (missing, restored, appearanceMappings);
    }

    internal static NpcDefinition[] Missing(
        IReadOnlyList<NpcDefinition> definitions,
        IReadOnlySet<int> existingIds) =>
        definitions.Where(definition => !existingIds.Contains(definition.Id)).ToArray();

    internal static NpcDefinition[] MissingStatuses(
        IReadOnlyList<NpcDefinition> definitions,
        IReadOnlyDictionary<int, NpcDefinition> existing) =>
        definitions.Where(definition =>
            existing.TryGetValue(definition.Id, out var value) && value.Status is null).ToArray();

    private static NpcStatusDefinition ToDefinition(NpcStatus status) => new(
        status.Attackable,
        status.Targetable,
        status.Talkable,
        status.Undying,
        status.ShowName,
        status.RandomWalk,
        status.CanMove,
        status.NoSleepMode,
        status.CanBeSown);

    private static NpcStatus ToEntity(string gameVersion, int npcId, NpcStatusDefinition status) => new()
    {
        GameVersion = gameVersion,
        NpcId = npcId,
        Attackable = status.Attackable,
        Targetable = status.Targetable,
        Talkable = status.Talkable,
        Undying = status.Undying,
        ShowName = status.ShowName,
        RandomWalk = status.RandomWalk,
        CanMove = status.CanMove,
        NoSleepMode = status.NoSleepMode,
        CanBeSown = status.CanBeSown
    };

    private static NpcStatsDefinition ToDefinition(NpcStats stats) => new(stats.Str, stats.Int, stats.Dex, stats.Wit, stats.Con, stats.Men, stats.HitTime);
    private static NpcStatsVitalsDefinition ToDefinition(NpcStatsVitals stats) => new(stats.Hp, stats.HpRegen, stats.Mp, stats.MpRegen);
    private static NpcStatsAttackDefinition ToDefinition(NpcStatsAttack stats) => new(stats.Physical, stats.Magical, stats.Random, stats.Critical, stats.Accuracy, stats.AttackSpeed, stats.ReuseDelay, stats.Type, stats.Range, stats.Distance, stats.Width);
    private static NpcStatsDefenceDefinition ToDefinition(NpcStatsDefence stats) => new(stats.Physical, stats.Magical, stats.Evasion, stats.Shield, stats.ShieldRate);
    private static NpcStatsSpeedDefinition ToDefinition(NpcStatsSpeed stats) => new(stats.WalkGround, stats.RunGround);

    private static NpcStats ToEntity(string gameVersion, int npcId, NpcStatsDefinition stats) => new()
    {
        GameVersion = gameVersion, NpcId = npcId, Str = stats.Str, Int = stats.Int, Dex = stats.Dex,
        Wit = stats.Wit, Con = stats.Con, Men = stats.Men, HitTime = stats.HitTime
    };

    private static NpcStatsVitals ToEntity(string gameVersion, int npcId, NpcStatsVitalsDefinition stats) => new()
    {
        GameVersion = gameVersion, NpcId = npcId, Hp = stats.Hp, HpRegen = stats.HpRegen, Mp = stats.Mp, MpRegen = stats.MpRegen
    };

    private static NpcStatsAttack ToEntity(string gameVersion, int npcId, NpcStatsAttackDefinition stats) => new()
    {
        GameVersion = gameVersion, NpcId = npcId, Physical = stats.Physical, Magical = stats.Magical,
        Random = stats.Random, Critical = stats.Critical, Accuracy = stats.Accuracy, AttackSpeed = stats.AttackSpeed,
        ReuseDelay = stats.ReuseDelay, Type = stats.Type, Range = stats.Range, Distance = stats.Distance, Width = stats.Width
    };

    private static NpcStatsDefence ToEntity(string gameVersion, int npcId, NpcStatsDefenceDefinition stats) => new()
    {
        GameVersion = gameVersion, NpcId = npcId, Physical = stats.Physical, Magical = stats.Magical,
        Evasion = stats.Evasion, Shield = stats.Shield, ShieldRate = stats.ShieldRate
    };

    private static NpcStatsSpeed ToEntity(string gameVersion, int npcId, NpcStatsSpeedDefinition stats) => new()
    {
        GameVersion = gameVersion, NpcId = npcId, WalkGround = stats.WalkGround, RunGround = stats.RunGround
    };

    internal static void ApplyStatus(GameContentDbContext context, Npc npc, NpcStatusDefinition status)
    {
        if (npc.Status is null)
        {
            npc.Status = ToEntity(npc.GameVersion, npc.Id, status);
            context.NpcStatuses.Add(npc.Status);
        }

        npc.Status.Attackable = status.Attackable;
        npc.Status.Targetable = status.Targetable;
        npc.Status.Talkable = status.Talkable;
        npc.Status.Undying = status.Undying;
        npc.Status.ShowName = status.ShowName;
        npc.Status.RandomWalk = status.RandomWalk;
        npc.Status.CanMove = status.CanMove;
        npc.Status.NoSleepMode = status.NoSleepMode;
        npc.Status.CanBeSown = status.CanBeSown;
    }

    private static void ApplyStats(GameContentDbContext context, Npc npc, NpcDefinition definition, bool removeMissing)
    {
        if (definition.Stats is not null && npc.Stats is null)
        {
            npc.Stats = ToEntity(npc.GameVersion, npc.Id, definition.Stats);
            context.NpcStats.Add(npc.Stats);
        }
        else if (definition.Stats is null && removeMissing && npc.Stats is not null)
        {
            context.NpcStats.Remove(npc.Stats);
            npc.Stats = null;
        }
        if (definition.Stats is not null && npc.Stats is not null)
        {
            npc.Stats.Str = definition.Stats.Str; npc.Stats.Int = definition.Stats.Int; npc.Stats.Dex = definition.Stats.Dex;
            npc.Stats.Wit = definition.Stats.Wit; npc.Stats.Con = definition.Stats.Con; npc.Stats.Men = definition.Stats.Men;
            npc.Stats.HitTime = definition.Stats.HitTime;
        }

        if (definition.StatsVitals is not null && npc.StatsVitals is null)
        {
            npc.StatsVitals = ToEntity(npc.GameVersion, npc.Id, definition.StatsVitals);
            context.NpcStatsVitals.Add(npc.StatsVitals);
        }
        else if (definition.StatsVitals is null && removeMissing && npc.StatsVitals is not null)
        {
            context.NpcStatsVitals.Remove(npc.StatsVitals);
            npc.StatsVitals = null;
        }
        if (definition.StatsVitals is not null && npc.StatsVitals is not null)
        {
            npc.StatsVitals.Hp = definition.StatsVitals.Hp; npc.StatsVitals.HpRegen = definition.StatsVitals.HpRegen;
            npc.StatsVitals.Mp = definition.StatsVitals.Mp; npc.StatsVitals.MpRegen = definition.StatsVitals.MpRegen;
        }

        if (definition.StatsAttack is not null && npc.StatsAttack is null)
        {
            npc.StatsAttack = ToEntity(npc.GameVersion, npc.Id, definition.StatsAttack);
            context.NpcStatsAttacks.Add(npc.StatsAttack);
        }
        else if (definition.StatsAttack is null && removeMissing && npc.StatsAttack is not null)
        {
            context.NpcStatsAttacks.Remove(npc.StatsAttack);
            npc.StatsAttack = null;
        }
        if (definition.StatsAttack is not null && npc.StatsAttack is not null)
        {
            npc.StatsAttack.Physical = definition.StatsAttack.Physical; npc.StatsAttack.Magical = definition.StatsAttack.Magical;
            npc.StatsAttack.Random = definition.StatsAttack.Random; npc.StatsAttack.Critical = definition.StatsAttack.Critical;
            npc.StatsAttack.Accuracy = definition.StatsAttack.Accuracy; npc.StatsAttack.AttackSpeed = definition.StatsAttack.AttackSpeed;
            npc.StatsAttack.ReuseDelay = definition.StatsAttack.ReuseDelay; npc.StatsAttack.Type = definition.StatsAttack.Type;
            npc.StatsAttack.Range = definition.StatsAttack.Range; npc.StatsAttack.Distance = definition.StatsAttack.Distance; npc.StatsAttack.Width = definition.StatsAttack.Width;
        }

        if (definition.StatsDefence is not null && npc.StatsDefence is null)
        {
            npc.StatsDefence = ToEntity(npc.GameVersion, npc.Id, definition.StatsDefence);
            context.NpcStatsDefences.Add(npc.StatsDefence);
        }
        else if (definition.StatsDefence is null && removeMissing && npc.StatsDefence is not null)
        {
            context.NpcStatsDefences.Remove(npc.StatsDefence);
            npc.StatsDefence = null;
        }
        if (definition.StatsDefence is not null && npc.StatsDefence is not null)
        {
            npc.StatsDefence.Physical = definition.StatsDefence.Physical; npc.StatsDefence.Magical = definition.StatsDefence.Magical;
            npc.StatsDefence.Evasion = definition.StatsDefence.Evasion; npc.StatsDefence.Shield = definition.StatsDefence.Shield;
            npc.StatsDefence.ShieldRate = definition.StatsDefence.ShieldRate;
        }

        if (definition.StatsSpeed is not null && npc.StatsSpeed is null)
        {
            npc.StatsSpeed = ToEntity(npc.GameVersion, npc.Id, definition.StatsSpeed);
            context.NpcStatsSpeeds.Add(npc.StatsSpeed);
        }
        else if (definition.StatsSpeed is null && removeMissing && npc.StatsSpeed is not null)
        {
            context.NpcStatsSpeeds.Remove(npc.StatsSpeed);
            npc.StatsSpeed = null;
        }
        if (definition.StatsSpeed is not null && npc.StatsSpeed is not null)
        {
            npc.StatsSpeed.WalkGround = definition.StatsSpeed.WalkGround;
            npc.StatsSpeed.RunGround = definition.StatsSpeed.RunGround;
        }
    }

    internal static string[] MissingC1Lookups(
        IReadOnlySet<string> types,
        IReadOnlySet<string> races,
        IReadOnlySet<string> sexes) =>
    [
        .. MissingLookupNames("NPC types", C1Catalog.Types.Select(definition => definition.Name), types),
        .. MissingLookupNames("NPC races", C1Catalog.Races.Select(definition => definition.Name), races),
        .. MissingLookupNames("NPC sexes", C1Catalog.Sexes.Select(definition => definition.Name), sexes)
    ];

    private static IEnumerable<string> MissingLookupNames(
        string label,
        IEnumerable<string> required,
        IReadOnlySet<string> existing)
    {
        var names = required.Where(name => !existing.Contains(name)).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        return names.Length == 0 ? [] : [$"{label} ({string.Join(", ", names)})"];
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

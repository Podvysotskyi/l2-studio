using L2.Studio.Context;
using L2.Studio.Context.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace L2.Studio.Worker.Tests;

public sealed class NpcLookupImportHandlersTests
{
    private static readonly NpcStatusDefinition DefaultStatus = new(
        true, true, true, false, true, true, true, false, false);

    private static readonly NpcLookupDefinition[] Definitions =
    [
        new("FIGHTER", "Fighter"),
        new("MAGE", "Mage")
    ];

    [Fact]
    public void AddMissingPreservesExistingDisplayNames()
    {
        var existing = new Dictionary<string, string>
        {
            ["FIGHTER"] = "Custom fighter",
            ["CUSTOM"] = "Custom"
        };

        var result = NpcLookupImportHandlers.Reconcile(Definitions, existing, false);

        Assert.Equal("MAGE", Assert.Single(result.Missing).Name);
        Assert.Empty(result.Restored);
    }

    [Fact]
    public void RestoreDefaultsResetsChangedBuiltInsAndPreservesExtras()
    {
        var existing = new Dictionary<string, string>
        {
            ["FIGHTER"] = "Custom fighter",
            ["MAGE"] = "Mage",
            ["CUSTOM"] = "Custom"
        };

        var result = NpcLookupImportHandlers.Reconcile(Definitions, existing, true);

        Assert.Empty(result.Missing);
        Assert.Equal("Fighter", Assert.Single(result.Restored).Value);
        Assert.DoesNotContain("CUSTOM", result.Restored);
    }

    [Fact]
    public void RestoreDefaultsReconcilesKnownNpcDefinitionsAndPreservesExtras()
    {
        var definitions = new NpcDefinition[]
        {
            new(1, 101, 10, "Goblin", "Monster", "HUMANOID", "MALE", DefaultStatus),
            new(2, 102, 20, "Orc", "Monster", "ORC", "MALE", DefaultStatus)
        };
        var existing = new Dictionary<int, NpcDefinition>
        {
            [1] = new(1, 99, 11, "Custom Goblin", "Custom", null, "FEMALE", null),
            [99] = new(99, 99, 1, "Custom", "Custom", null, "NONE", DefaultStatus)
        };

        var result = NpcImportHandlers.Reconcile(definitions, existing, true);

        Assert.Equal(2, Assert.Single(result.Missing).Id);
        Assert.Equal(definitions[0], Assert.Single(result.Restored));
        Assert.Equal(definitions[0], Assert.Single(result.AppearanceMappings));
        Assert.DoesNotContain(result.Restored, definition => definition.Id == 99);
    }

    [Fact]
    public void AddMissingDoesNotRestoreCustomizedNpcDefinitions()
    {
        var definitions = new NpcDefinition[]
        {
            new(1, 101, 10, "Goblin", "Monster", "HUMANOID", "MALE", DefaultStatus)
        };
        var existing = new Dictionary<int, NpcDefinition>
        {
            [1] = new(1, 99, 11, "Custom Goblin", "Custom", null, "FEMALE", null)
        };

        var result = NpcImportHandlers.Reconcile(definitions, existing, false);

        Assert.Empty(result.Missing);
        Assert.Empty(result.Restored);
        Assert.Equal(definitions[0], Assert.Single(result.AppearanceMappings));
    }

    [Fact]
    public void AddMissingFindsExistingNpcDefinitionsWithoutStatuses()
    {
        var definitions = new NpcDefinition[]
        {
            new(1, 101, 10, "Goblin", "Monster", "HUMANOID", "MALE", DefaultStatus),
            new(2, 102, 20, "Orc", "Monster", "ORC", "MALE", DefaultStatus)
        };
        var existing = new Dictionary<int, NpcDefinition>
        {
            [1] = definitions[0] with { Status = null },
            [2] = definitions[1],
            [99] = new(99, 99, 1, "Custom", "Custom", null, "NONE", null)
        };

        var result = NpcImportHandlers.MissingStatuses(definitions, existing);

        Assert.Equal(1, Assert.Single(result).Id);
    }

    [Fact]
    public void RestoreDefaultsReconcilesChangedNpcStatuses()
    {
        var definition = new NpcDefinition(1, 101, 10, "Goblin", "Monster", "HUMANOID", "MALE", DefaultStatus);
        var existing = new Dictionary<int, NpcDefinition>
        {
            [1] = definition with { Status = DefaultStatus with { Attackable = false } }
        };

        var result = NpcImportHandlers.Reconcile([definition], existing, true);

        Assert.Equal(definition, Assert.Single(result.Restored));
    }

    [Fact]
    public void ApplyingAMissingStatusExplicitlyAddsTheDependent()
    {
        using var context = CreateContext();
        var npc = Npc(1);
        context.Npcs.Attach(npc);

        NpcImportHandlers.ApplyStatus(context, npc, DefaultStatus);

        Assert.Equal(EntityState.Added, context.Entry(npc.Status!).State);
    }

    [Fact]
    public void ApplyingAnExistingStatusUpdatesTheDependent()
    {
        using var context = CreateContext();
        var npc = Npc(1);
        npc.Status = new NpcStatus
        {
            GameVersion = npc.GameVersion,
            NpcId = npc.Id,
            Attackable = false,
            Targetable = DefaultStatus.Targetable,
            Talkable = DefaultStatus.Talkable,
            Undying = DefaultStatus.Undying,
            ShowName = DefaultStatus.ShowName,
            RandomWalk = DefaultStatus.RandomWalk,
            CanMove = DefaultStatus.CanMove,
            NoSleepMode = DefaultStatus.NoSleepMode,
            CanBeSown = DefaultStatus.CanBeSown
        };
        context.Npcs.Attach(npc);

        NpcImportHandlers.ApplyStatus(context, npc, DefaultStatus);

        Assert.Equal(EntityState.Modified, context.Entry(npc.Status).State);
    }

    [Fact]
    public void C1CatalogContainsEveryMobiusNpcAndAuthoritativeAppearanceMappings()
    {
        var npcs = new C1NpcLookupCatalog().Npcs;

        Assert.Equal(1893, npcs.Count);
        Assert.Equal(npcs.Count, npcs.Select(npc => npc.Id).Distinct().Count());
        Assert.Equal(1, Assert.Single(npcs, npc => npc.Id == 20001).AppearanceId);
        Assert.Equal(7001, Assert.Single(npcs, npc => npc.Id == 30001).AppearanceId);
        Assert.Equal(12077, Assert.Single(npcs, npc => npc.Id == 12077).AppearanceId);
        Assert.Equal(7217, Assert.Single(npcs, npc => npc.Id == 501).AppearanceId);
        Assert.Equal(85, Assert.Single(npcs, npc => npc.Id == 500).Level);
    }

    [Fact]
    public void C1CatalogResolvesMobiusNpcStatuses()
    {
        var npcs = new C1NpcLookupCatalog().Npcs;

        var gremlin = Assert.Single(npcs, npc => npc.Id == 20001).Status!;
        Assert.True(gremlin.CanBeSown);
        Assert.False(gremlin.Undying);

        var guard = Assert.Single(npcs, npc => npc.Id == 501).Status!;
        Assert.False(guard.RandomWalk);
        Assert.False(guard.Attackable);

        var queenAnt = Assert.Single(npcs, npc => npc.Id == 29001).Status!;
        Assert.True(queenAnt.NoSleepMode);
        Assert.False(queenAnt.RandomWalk);

        var eventNpc = Assert.Single(npcs, npc => npc.Id == 82016).Status!;
        Assert.False(eventNpc.Attackable);
        Assert.True(eventNpc.ShowName);
        Assert.False(eventNpc.CanMove);

        var noStatusNpc = Assert.Single(npcs, npc => npc.Id == 27165).Status!;
        Assert.True(noStatusNpc.Attackable);
        Assert.False(noStatusNpc.Undying);
    }

    [Fact]
    public void C1CatalogPreservesRawMobiusNpcStats()
    {
        var npcs = new C1NpcLookupCatalog().Npcs;

        var wolf = Assert.Single(npcs, npc => npc.Id == 12077);
        Assert.Equal(40, wolf.Stats!.Str);
        Assert.Equal(25, wolf.Stats.Men);
        Assert.Equal(246.95422m, wolf.StatsVitals!.Hp);
        Assert.Equal(29.61691m, wolf.StatsAttack!.Physical);
        Assert.Equal("FIST", wolf.StatsAttack.Type);
        Assert.Equal(73.55216m, wolf.StatsDefence!.Physical);
        Assert.Equal(24m, wolf.StatsSpeed!.WalkGround);
        Assert.Equal(125m, wolf.StatsSpeed.RunGround);

        var guard = Assert.Single(npcs, npc => npc.Id == 501);
        Assert.Null(guard.Stats!.Str);
        Assert.Equal(1500, guard.StatsAttack!.ReuseDelay);
    }

    [Fact]
    public void C1CatalogPreservesRawMobiusNpcHitTime()
    {
        var npcs = new C1NpcLookupCatalog().Npcs;

        Assert.Equal(370, Assert.Single(npcs, npc => npc.Id == 20001).Stats!.HitTime);
        Assert.Null(Assert.Single(npcs, npc => npc.Id == 20002).Stats!.HitTime);
    }

    private static Npc Npc(int id) => new()
    {
        GameVersion = "c1",
        Id = id,
        Level = 1,
        NpcTypeName = "Monster",
        NpcSexName = "MALE"
    };

    private static GameContentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql("Host=localhost;Database=model;Username=model;Password=model")
            .Options;
        return new GameContentDbContext(options);
    }
}

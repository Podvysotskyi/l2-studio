using L2.Studio.Configurations;
using L2.Studio.Context.Entities;
using L2.Studio.Context.Identifiers;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine.Attributes;
using Xunit;

namespace L2.Studio.Worker.Tests;

public sealed class WorkerJobTests
{
    [Fact]
    public void DefinesVersionSpecificNpcLookupCatalogs()
    {
        NpcLookupCatalog c1 = new C1NpcLookupCatalog();
        NpcLookupCatalog c4 = new C4NpcLookupCatalog();
        NpcLookupCatalog interlude = new InterludeNpcLookupCatalog();

        Assert.Equal(28, c1.Types.Count);
        Assert.Equal(46, c4.Types.Count);
        Assert.Equal(48, interlude.Types.Count);
        Assert.Equal(21, c1.Races.Count);
        Assert.Equal(22, c4.Races.Count);
        Assert.Equal(22, interlude.Races.Count);
        Assert.Equal(["MALE", "FEMALE", "ETC"], c1.Sexes.Select(item => item.Name));
        Assert.Equal(c1.Sexes, c4.Sexes);
        Assert.Equal(c1.Sexes, interlude.Sexes);
        Assert.DoesNotContain(c1.Races, item => item.Name == "DIVINE");
        Assert.Contains(c4.Races, item => item.Name == "DIVINE");
        Assert.DoesNotContain(interlude.Races, item => item.Name == "NONE");
    }

    [Fact]
    public void DefinesCompleteImportableC1NpcCatalog()
    {
        NpcLookupCatalog catalog = new C1NpcLookupCatalog();

        Assert.Equal(1893, catalog.Npcs.Count);
        Assert.Equal(catalog.Npcs.Count, catalog.Npcs.Select(npc => npc.Id).Distinct().Count());
        Assert.Contains(catalog.Npcs, npc => npc is
        {
            Id: 20003, Level: 5, Name: "Goblin", TypeName: "Monster", RaceName: "HUMANOID", SexName: "MALE"
        });
        Assert.Contains(catalog.Npcs, npc => npc is
        {
            Id: 80000, Level: 78, Name: "Evi", TypeName: "Folk", RaceName: "DARK_ELF", SexName: "FEMALE"
        });
        Assert.Contains(catalog.Npcs, npc => npc is
        {
            Id: 500, AppearanceId: 7115, Level: 85, Name: "Jurek", TypeName: "Folk", RaceName: "HUMAN", SexName: "MALE"
        });
        Assert.Contains(catalog.Npcs, npc => npc is
        {
            Id: 900100, AppearanceId: 432, Level: 1, Name: "Elpy", TypeName: "EventMonster", RaceName: null, SexName: "ETC"
        });
        Assert.All(catalog.Npcs, npc =>
        {
            Assert.InRange(npc.Level, (short)1, (short)255);
            Assert.Contains(catalog.Types, type => type.Name == npc.TypeName);
            if (npc.RaceName is not null) Assert.Contains(catalog.Races, race => race.Name == npc.RaceName);
            Assert.Contains(catalog.Sexes, sex => sex.Name == npc.SexName);
        });
    }

    [Fact]
    public void DefinesCompleteImportableC1SkillCatalog()
    {
        var catalog = new C1SkillCatalog();

        Assert.Equal(584, catalog.Skills.Count);
        Assert.Equal(catalog.Skills.Count, catalog.Skills.Select(skill => skill.Id).Distinct().Count());
        Assert.Equal(7, catalog.OperateTypes.Count);
        Assert.Equal(27, catalog.TargetTypes.Count);
        Assert.Contains(catalog.OperateTypes, value => value is { Name: "A1", DisplayName: "A1" });
        Assert.Contains(catalog.TargetTypes, value => value is
            { Name: "AREA_CORPSE_MOB", DisplayName: "Area Corpse Mob" });
        Assert.Contains(catalog.Skills, skill => skill is
        {
            Id: 1, Levels: 37, Name: "Triple Slash", Icons.Count: 37
        });
        Assert.Contains(catalog.Skills, skill => skill is
        {
            Id: 4071, Name: "Resist Archery", Icons.Count: 5
        });
        Assert.All(catalog.Skills, skill =>
        {
            Assert.InRange(skill.Levels, (short)1, (short)255);
            Assert.All(skill.Icons, icon => Assert.InRange(icon.Level, (short)1, skill.Levels));
            if (skill.OperateTypeName is not null)
                Assert.Contains(catalog.OperateTypes, value => value.Name == skill.OperateTypeName);
            if (skill.TargetTypeName is not null)
                Assert.Contains(catalog.TargetTypes, value => value.Name == skill.TargetTypeName);
        });
    }

    [Fact]
    public void DefinesCompleteImportableC1ItemLookupCatalog()
    {
        ItemLookupCatalog catalog = new C1ItemCatalog();

        Assert.Equal(25, catalog.Types.Count);
        Assert.Equal(6, catalog.Actions.Count);
        Assert.Equal(14, catalog.BodyParts.Count);
        Assert.Equal(23, catalog.Materials.Count);
        Assert.Equal(5, catalog.CrystalTypes.Count);
        Assert.Equal(10, catalog.Handlers.Count);
        Assert.Equal(2, catalog.SkillTypes.Count);
        Assert.Equal(158, ((C1ItemCatalog)catalog).Items.Sum(item => item.Skills.Count));
        Assert.Contains(catalog.Types, definition => definition is { Name: "Weapon", DisplayName: "Weapon" });
        Assert.Contains(catalog.Types, definition => definition is
            { Name: "SWORD", DisplayName: "Sword", ParentTypeName: "Weapon" });
        Assert.Contains(catalog.Types, definition => definition is
            { Name: "HEAVY", DisplayName: "Heavy", ParentTypeName: "Armor" });
        Assert.Contains(catalog.Types, definition => definition is
            { Name: "RECIPE", DisplayName: "Recipe", ParentTypeName: "EtcItem" });
        Assert.Contains(catalog.Actions, definition => definition is { Name: "SKILL_MAINTAIN", DisplayName: "Skill Maintain" });
        Assert.Contains(catalog.Materials, definition => definition is { Name: "SCALE_OF_DRAGON", DisplayName: "Scale Of Dragon" });
        Assert.Contains(catalog.BodyParts, definition => definition is { Name: "lhand", DisplayName: "Left Hand" });
        Assert.Contains(catalog.BodyParts, definition => definition is { Name: "rhand", DisplayName: "Right Hand" });
        Assert.Contains(catalog.BodyParts, definition => definition is { Name: "hands", DisplayName: "Two Hands" });
        Assert.Contains(catalog.BodyParts, definition => definition is { Name: "ear", DisplayName: "Ear" });
        Assert.Contains(catalog.BodyParts, definition => definition is { Name: "finger", DisplayName: "Finger" });
        Assert.Contains(catalog.Handlers, definition => definition is { Name: "ItemSkills", DisplayName: "Item Skills" });
        Assert.Contains(catalog.SkillTypes, definition => definition is { Name: "ON_CRITICAL_SKILL", DisplayName: "On Critical Skill" });
        Assert.DoesNotContain(catalog.BodyParts, definition => definition.Name is "lrhand" or "hand" or "rear;lear" or "rfinger;lfinger");
        Assert.Contains(((C1ItemCatalog)catalog).Items, item => item is
        {
            Id: 3028,
            AttackGeometry: { OffsetX: 0, OffsetY: 0, Radius: 10, Length: 0 }
        });
        Assert.Contains(((C1ItemCatalog)catalog).Items, item => item is
        {
            Id: 3027,
            AttackGeometry: { OffsetX: 0, OffsetY: 0, Radius: 44, Length: 120 }
        });
        Assert.Contains(((C1ItemCatalog)catalog).Items, item => item is
        {
            Id: 1660,
            Skills: [{ SkillId: 3005, SkillLevel: 1, TypeName: "ON_CRITICAL_SKILL", Chance: 50 }]
        });

        Assert.All(((C1ItemCatalog)catalog).Items, item =>
        {
            Assert.Contains(catalog.Types, definition => definition.Name == item.TypeName);
            if (item.ActionName is not null) Assert.Contains(catalog.Actions, definition => definition.Name == item.ActionName);
            if (item.BodyPartName is not null) Assert.Contains(catalog.BodyParts, definition => definition.Name == item.BodyPartName);
            if (item.MaterialName is not null) Assert.Contains(catalog.Materials, definition => definition.Name == item.MaterialName);
            if (item.CrystalTypeName is not null) Assert.Contains(catalog.CrystalTypes, definition => definition.Name == item.CrystalTypeName);
            if (item.HandlerName is not null) Assert.Contains(catalog.Handlers, definition => definition.Name == item.HandlerName);
            Assert.All(item.Skills, skill =>
            {
                if (skill.TypeName is not null) Assert.Contains(catalog.SkillTypes, definition => definition.Name == skill.TypeName);
            });
        });
        Assert.Contains(((C1ItemCatalog)catalog).Items, item => item is { Id: 1, TypeName: "SWORD" });
        Assert.Contains(((C1ItemCatalog)catalog).Items, item => item is { Id: 1119, TypeName: "Armor" });
        Assert.Contains(((C1ItemCatalog)catalog).Items, item => item is { Id: 1118, TypeName: "EtcItem" });
    }

    [Fact]
    public void DefinesCompleteImportableC1PlayerCatalog()
    {
        var catalog = new C1PlayerCatalog();

        Assert.Equal(5, catalog.Races.Count);
        Assert.Equal(2, catalog.Sexes.Count);
        Assert.Equal(89, catalog.Classes.Count);
        Assert.Equal(30, catalog.Faces.Count);
        Assert.Equal(60, catalog.HairStyles.Count);
        Assert.Equal(40, catalog.HairColors.Count);
        Assert.Contains(catalog.Classes, value => value is
        {
            Id: PlayerClassId.HumanFighter, RaceId: PlayerRaceId.Human, IsMage: false, ParentClassId: null
        });
        Assert.Contains(catalog.Classes, value => value is
        {
            Id: PlayerClassId.Archmage, RaceId: PlayerRaceId.Human, IsMage: true, ParentClassId: PlayerClassId.Sorcerer
        });
    }

    [Fact]
    public void ImportsOnlyNpcIdsMissingFromTheCatalog()
    {
        var definitions = new[]
        {
            new NpcDefinition(1, 1, 1, "Gremlin", "Monster", "FAIRY", "MALE", null),
            new NpcDefinition(2, 2, 2, "Fox", "Monster", "ANIMAL", "MALE", null)
        };

        var missing = NpcImportHandlers.Missing(definitions, new HashSet<int> { 1, 99 });

        Assert.Equal(2, Assert.Single(missing).Id);
    }

    [Fact]
    public void IdentifiesMissingC1NpcLookupPrerequisites()
    {
        var missing = NpcImportHandlers.MissingC1Lookups(
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(["HUMAN"], StringComparer.Ordinal),
            new HashSet<string>(["MALE", "FEMALE", "ETC"], StringComparer.Ordinal));

        Assert.Contains(missing, value => value.StartsWith("NPC types (", StringComparison.Ordinal));
        Assert.Contains(missing, value => value.Contains("NPC races (ANIMAL", StringComparison.Ordinal));
        Assert.DoesNotContain(missing, value => value.StartsWith("NPC sexes (", StringComparison.Ordinal));
    }

    [Fact]
    public void IdentifiesMissingC1ItemLookupPrerequisites()
    {
        var catalog = new C1ItemCatalog();
        var missing = ItemImportHandlers.MissingC1Lookups(
            new HashSet<string>(catalog.Types.Select(definition => definition.Name), StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(catalog.BodyParts.Select(definition => definition.Name), StringComparer.Ordinal),
            new HashSet<string>(catalog.Materials.Select(definition => definition.Name), StringComparer.Ordinal),
            new HashSet<string>(catalog.CrystalTypes.Select(definition => definition.Name), StringComparer.Ordinal),
            new HashSet<string>(catalog.Handlers.Select(definition => definition.Name), StringComparer.Ordinal),
            new HashSet<string>(catalog.SkillTypes.Select(definition => definition.Name), StringComparer.Ordinal));

        Assert.Contains(missing, value => value.StartsWith("item actions (", StringComparison.Ordinal));
        Assert.DoesNotContain(missing, value => value.StartsWith("item types (", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("SIEGE_WEAPON", "Siege Weapon")]
    [InlineData("HUMAN", "Human")]
    [InlineData("VillageMasterFighter", "Village Master Fighter")]
    [InlineData("VillageMasterDElf", "Village Master Dark Elf")]
    [InlineData("mixed_case", "Mixed Case")]
    public void GeneratesFriendlyNpcLookupNames(string source, string expected) =>
        Assert.Equal(expected, NpcLookupCatalog.FriendlyName(source));

    [Fact]
    public void KeepsEveryWolverineHandlerInWorker()
    {
        var workerAssembly = typeof(NpcLookupImportHandlers).Assembly;
        var handlerTypes = workerAssembly.GetTypes()
            .Where(type => type.GetCustomAttributes(typeof(WolverineHandlerAttribute), inherit: false).Length > 0)
            .ToArray();

        Assert.Equal(12, handlerTypes.Length);
        Assert.All(handlerTypes, type => Assert.Equal("L2.Studio.Worker", type.Namespace));
        Assert.DoesNotContain(typeof(AssetImportJobProcessor).Assembly.GetTypes(), type =>
            type.GetCustomAttributes(typeof(WolverineHandlerAttribute), inherit: false).Length > 0);
    }

    [Fact]
    public void AggregatesRunCountsAndWarningsByTerminalFile()
    {
        var run = Run(
            Item(AssetImportJobValues.Succeeded),
            Item(AssetImportJobValues.SucceededWithWarnings, warnings: 2),
            Item(AssetImportJobValues.Failed),
            Item(AssetImportJobValues.Running));

        AssetImportRunHandlers.ApplyCounts(run);

        Assert.Equal(3, run.CompletedFileCount);
        Assert.Equal(2, run.SucceededFileCount);
        Assert.Equal(1, run.WarningFileCount);
        Assert.Equal(1, run.FailedFileCount);
    }

    [Fact]
    public void ResetsRunCountsWhenNoWorkItemsExist()
    {
        var run = Run();
        run.CompletedFileCount = 10;
        run.SucceededFileCount = 9;
        run.WarningFileCount = 8;
        run.FailedFileCount = 7;

        AssetImportRunHandlers.ApplyCounts(run);

        Assert.Equal(0, run.CompletedFileCount);
        Assert.Equal(0, run.SucceededFileCount);
        Assert.Equal(0, run.WarningFileCount);
        Assert.Equal(0, run.FailedFileCount);
    }

    [Fact]
    public void FinalizesOnlyActiveRunsAfterDiscoveryAndAllWorkCompletes()
    {
        var run = Run(Item(AssetImportJobValues.Succeeded), Item(AssetImportJobValues.Reused));
        run.DiscoveredFileCount = 2;

        AssetImportRunHandlers.ApplyCounts(run);

        Assert.False(AssetImportRunHandlers.IsReadyToFinalize(run));

        run.DiscoveryFinishedAt = DateTimeOffset.UtcNow;

        Assert.True(AssetImportRunHandlers.IsReadyToFinalize(run));

        run.Status = AssetImportJobValues.Succeeded;

        Assert.False(AssetImportRunHandlers.IsReadyToFinalize(run));
    }

    [Fact]
    public void RegistersWorkerJobsAndReconciliationPublisher()
    {
        var apiBuilder = CreateHostBuilder();
        apiBuilder.AddStudioApiMessaging();
        Assert.DoesNotContain(apiBuilder.Services, HostedService<AssetStorageReconciliationPublisher>);

        var workerBuilder = CreateHostBuilder(Environments.Development);
        workerBuilder.AddStudioWorker("l2-studio-worker");
        workerBuilder.AddStudioWorkerJobs();
        workerBuilder.Services.AddStudioWorkerApplication(workerBuilder.Configuration);

        Assert.Contains(workerBuilder.Services, HostedService<AssetStorageReconciliationPublisher>);
        using var host = workerBuilder.Build();
    }

    private static AssetImportRun Run(params AssetImportWorkItem[] workItems) => new()
    {
        Id = Guid.NewGuid(),
        Kind = AssetImportJobValues.Textures,
        TriggerType = AssetImportJobValues.FullScan,
        Status = AssetImportJobValues.Running,
        RequestedAt = DateTimeOffset.UtcNow,
        WorkItems = workItems
    };

    private static AssetImportWorkItem Item(string status, int warnings = 0) => new()
    {
        Id = Guid.NewGuid(),
        ImportKind = AssetImportJobValues.Textures,
        SourceKey = $"{Guid.NewGuid():N}.utx",
        NormalizedSourceKey = Guid.NewGuid().ToString("N"),
        SourcePath = "/tmp/source.utx",
        Status = status,
        WarningCount = warnings,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static HostApplicationBuilder CreateHostBuilder(string environmentName = "Testing")
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environmentName
        });
        builder.Configuration["ConnectionStrings:PostgreSql"] =
            "Host=localhost;Database=studio;Username=studio;Password=studio";
        return builder;
    }

    private static bool HostedService<TImplementation>(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(IHostedService) &&
        descriptor.ImplementationType == typeof(TImplementation);
}

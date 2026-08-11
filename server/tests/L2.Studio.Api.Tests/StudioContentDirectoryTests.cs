using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Context.Identifiers;
using L2.Studio.Api;
using L2.Studio.Contracts;
using L2.Studio.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace L2.Studio.Api.Tests;

[Collection(PostgreSqlIntegrationCollection.Name)]
public sealed class StudioContentDirectoryTests : IAsyncLifetime
{
    private readonly PostgreSqlIntegrationFixture postgres;
    private PostgreSqlDatabaseLease? database;
    private StudioFactory? factory;

    public StudioContentDirectoryTests(PostgreSqlIntegrationFixture postgres) => this.postgres = postgres;

    public async Task InitializeAsync()
    {
        database = await postgres.CreateDatabaseAsync();
        factory = new StudioFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        (await client.GetAsync("/health/live")).EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        if (factory is not null) await factory.DisposeAsync();
        if (database is not null) await database.DisposeAsync();
    }

    [Fact]
    public async Task Directory_lists_seeded_lookups_and_paginated_npcs()
    {
        await SeedNpcsAsync();
        using var client = factory!.CreateClient();

        var races = await client.GetFromJsonAsync<IReadOnlyList<NpcLookupSummary>>(
            "/api/content/npc-races");
        var sexes = await client.GetFromJsonAsync<IReadOnlyList<NpcLookupSummary>>(
            "/api/content/npc-sexes");
        var types = await client.GetFromJsonAsync<IReadOnlyList<NpcLookupSummary>>(
            "/api/content/npc-types");
        Assert.Equal(22, races!.Count);
        Assert.Equal("HUMAN", races[0].Name);
        Assert.Equal(3, sexes!.Count);
        Assert.Equal("MALE", sexes[0].Name);
        Assert.Equal(48, types!.Count);
        Assert.Equal("Adventurer", types[0].Name);

        var page = await client.GetFromJsonAsync<NpcDirectoryPage>(
            "/api/content/npcs?query=goblin&page=1&pageSize=1");
        Assert.NotNull(page);
        Assert.Equal(2, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(1001, page.Items[0].Id);
        Assert.Equal("Goblin Scout", page.Items[0].Name);
        Assert.Equal("Monster", page.Items[0].NpcType);
        Assert.Equal("HUMANOID", page.Items[0].NpcRace);
        Assert.Equal("MALE", page.Items[0].NpcSex);

        var secondPage = await client.GetFromJsonAsync<NpcDirectoryPage>(
            "/api/content/npcs?query=GOBLIN&page=2&pageSize=1");
        Assert.Equal(1002, secondPage!.Items[0].Id);

        var completePage = await client.GetFromJsonAsync<NpcDirectoryPage>(
            "/api/content/npcs?page=1&pageSize=10");
        var unclassifiedNpc = Assert.Single(completePage!.Items, item => item.Id == 2000);
        Assert.Null(unclassifiedNpc.Name);
        Assert.Null(unclassifiedNpc.NpcRaceId);
        Assert.Null(unclassifiedNpc.NpcRace);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?query=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Npc_directory_validates_query_parameters(string query)
    {
        using var client = factory!.CreateClient();
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.GetAsync($"/api/content/npcs{query}")).StatusCode);
    }

    [Fact]
    public async Task Directory_lists_the_seeded_player_class_hierarchy()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql(database!.ConnectionString)
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            context.PlayerClasses.Add(new PlayerClass
            {
                Id = PlayerClassId.HumanFighter,
                PlayerSexId = PlayerSexId.Male,
                PlayerRaceId = PlayerRaceId.Elf,
                Name = "Human Fighter"
            });
            await context.SaveChangesAsync();
        }

        using var client = factory!.CreateClient();

        var playerClasses = await client.GetFromJsonAsync<IReadOnlyList<PlayerClassSummary>>(
            "/api/content/player-classes");

        Assert.NotNull(playerClasses);
        Assert.Equal(89, playerClasses.Count);
        Assert.Equal(9, playerClasses.Count(playerClass => playerClass.ParentClassId is null));
        var humanFighter = Assert.Single(playerClasses, playerClass => playerClass.Id == 0);
        Assert.Equal("Human Fighter", humanFighter.Name);
        Assert.Null(humanFighter.ParentClassId);
        Assert.False(humanFighter.IsMage);
        Assert.Equal(2, humanFighter.AllowedRaces.Count);
        var humanRace = humanFighter.AllowedRaces[0];
        Assert.Equal(0, humanRace.Id);
        Assert.Equal("Human", humanRace.Name);
        Assert.Equal(
            [(0, "Male"), (1, "Female")],
            humanRace.AllowedSexes.Select(sex => (sex.Id, sex.Name)));
        var elfRace = humanFighter.AllowedRaces[1];
        Assert.Equal(1, elfRace.Id);
        Assert.Equal("Elf", elfRace.Name);
        Assert.Equal([(0, "Male")], elfRace.AllowedSexes.Select(sex => (sex.Id, sex.Name)));

        var gladiator = Assert.Single(playerClasses, playerClass => playerClass.Id == 2);
        Assert.Equal("Gladiator", gladiator.Name);
        Assert.Equal(1, gladiator.ParentClassId);

        var duelist = Assert.Single(playerClasses, playerClass => playerClass.Id == 88);
        Assert.Equal("Duelist", duelist.Name);
        Assert.Equal(2, duelist.ParentClassId);
        Assert.True(Assert.Single(playerClasses, playerClass => playerClass.Id == 10).IsMage);
    }

    [Fact]
    public async Task Directory_lists_player_lookups_separately_from_npc_lookups()
    {
        using var client = factory!.CreateClient();
        var races = await client.GetFromJsonAsync<IReadOnlyList<PlayerLookupSummary>>(
            "/api/content/player-races");
        var sexes = await client.GetFromJsonAsync<IReadOnlyList<PlayerLookupSummary>>(
            "/api/content/player-sexes");

        Assert.Equal([(0, "Human"), (1, "Elf"), (2, "Dark Elf"), (3, "Orc"), (4, "Dwarf")],
            races!.Select(value => (value.Id, value.Name)));
        Assert.Equal([(0, "Male"), (1, "Female")],
            sexes!.Select(value => (value.Id, value.Name)));
    }

    [Theory]
    [InlineData("systextures")]
    [InlineData("textures")]
    [InlineData("music")]
    [InlineData("staticmeshes")]
    [InlineData("levels")]
    [InlineData("scenes")]
    public async Task Asset_import_endpoint_queues_one_active_job_per_kind(string kind)
    {
        using var client = factory!.CreateClient();

        var queueResponses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => client.PostAsync($"/api/assets/{kind}/imports", null)));
        var queuedResponse = Assert.Single(queueResponses, response => response.StatusCode == HttpStatusCode.Accepted);
        Assert.Equal(7, queueResponses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(HttpStatusCode.Accepted, queuedResponse.StatusCode);
        var queued = await queuedResponse.Content.ReadFromJsonAsync<AssetImportJobSummary>();
        Assert.NotNull(queued);
        Assert.Equal(kind, queued.Kind);
        Assert.Equal("queued", queued.Status);

        var jobs = await client.GetFromJsonAsync<IReadOnlyList<AssetImportJobSummary>>(
            $"/api/assets/{kind}/imports?limit=10");
        Assert.Contains(jobs!, job => job.Id == queued.Id);

        var byId = await client.GetFromJsonAsync<AssetImportJobSummary>(
            $"/api/assets/{kind}/imports/{queued.Id}");
        Assert.Equal(queued.Id, byId!.Id);

        var otherKind = kind == "textures" ? "systextures" : "textures";
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/assets/{otherKind}/imports/{queued.Id}")).StatusCode);
    }

    [Fact]
    public async Task Level_preview_import_can_target_one_catalog_level()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql(database!.ConnectionString)
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            await AssetCatalogPublisher.PublishAsync(
                context, Guid.NewGuid(), "levels", "maps", new string('a', 64), 5, 111,
                Array.Empty<string>(), group => group,
                new[] { new { name = "16_25", status = "resolved" } },
                item => item.name, _ => (string?)null, item => item.status,
                new { }, DateTimeOffset.UtcNow, CancellationToken.None);
        }

        using var client = factory!.CreateClient();
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PostAsync("/api/assets/levels/imports?levelName=16_25", null)).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PostAsync("/api/assets/levelpreviews/imports?levelName=../16_25", null)).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.PostAsync("/api/assets/levelpreviews/imports?levelName=17_25", null)).StatusCode);

        var response = await client.PostAsync(
            "/api/assets/levelpreviews/imports?levelName=16_25",
            null);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var job = await response.Content.ReadFromJsonAsync<AssetImportJobSummary>();
        Assert.NotNull(job);
        Assert.Equal("levelpreviews", job.Kind);
        Assert.EndsWith(Path.Combine("maps", "16_25.unr"), job.SourcePath);
    }

    [Fact]
    public async Task Asset_catalog_endpoint_returns_summaries_filtered_pages_and_exact_entries()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql(database!.ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", GameContentDbContext.SchemaName))
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            await AssetCatalogPublisher.PublishAsync(
                context, Guid.NewGuid(), "textures", "textures", new string('a', 64), 4, 121,
                new[] { new { name = "Interface", textureCount = 2 } }, group => group.name,
                new[]
                {
                    new { packageName = "Interface", objectName = "IconSword", status = "resolved" },
                    new { packageName = "Interface", objectName = "IconShield", status = "skipped" }
                }, item => item.objectName, item => item.packageName, item => item.status,
                new { }, DateTimeOffset.UtcNow, CancellationToken.None);
        }

        using var client = factory!.CreateClient();
        var summaries = await client.GetFromJsonAsync<IReadOnlyList<AssetCatalogSummary>>("/api/assets/catalogs");
        var summary = Assert.Single(summaries!);
        Assert.Equal(2, summary.Total);
        Assert.Equal(1, summary.Resolved);
        Assert.Equal(1, summary.Skipped);

        var page = await client.GetFromJsonAsync<AssetCatalogPage>("/api/assets/textures/catalog?query=shield&page=1&pageSize=1");
        Assert.NotNull(page);
        Assert.Equal(1, page.Total);
        Assert.Equal("IconShield", Assert.Single(page.Items).GetProperty("objectName").GetString());
        Assert.Single(page.Groups);

        var exact = await client.GetFromJsonAsync<JsonElement>("/api/assets/textures/catalog/IconSword");
        Assert.Equal("resolved", exact.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/assets/textures/catalog?pageSize=501")).StatusCode);
    }

    [Fact]
    public async Task Directory_lists_skill_lookups_and_paginated_skills()
    {
        await SeedSkillsAsync();
        using var client = factory!.CreateClient();

        var operateTypes = await client.GetFromJsonAsync<IReadOnlyList<SkillLookupSummary>>(
            "/api/content/skill-operate-types");
        var targetTypes = await client.GetFromJsonAsync<IReadOnlyList<SkillLookupSummary>>(
            "/api/content/skill-target-types");
        Assert.Equal("A1", Assert.Single(operateTypes!).Name);
        Assert.Equal("ONE", Assert.Single(targetTypes!).Name);

        var page = await client.GetFromJsonAsync<SkillDirectoryPage>(
            "/api/content/skills?query=slash&page=1&pageSize=1");
        Assert.NotNull(page);
        Assert.Equal(2, page.Total);
        var skill = Assert.Single(page.Items);
        Assert.Equal(1, skill.Id);
        Assert.Equal("Triple Slash", skill.Name);
        Assert.Equal(37, skill.Levels);
        Assert.Equal("A1", skill.SkillOperateType);
        Assert.Equal("ONE", skill.SkillTargetType);
        Assert.Equal(1, skill.IconCount);

        var secondPage = await client.GetFromJsonAsync<SkillDirectoryPage>(
            "/api/content/skills?query=SLASH&page=2&pageSize=1");
        Assert.Equal(5, secondPage!.Items[0].Id);
    }

    [Fact]
    public async Task Skill_directory_validates_query_parameters()
    {
        using var client = factory!.CreateClient();
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/content/skills?pageSize=101")).StatusCode);
    }

    private async Task SeedNpcsAsync()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql(database!.ConnectionString)
            .Options;
        await using var context = new GameContentDbContext(options);
        context.Npcs.AddRange(
            new Npc
            {
                Id = 1001,
                Level = 8,
                Name = "Goblin Scout",
                NpcTypeId = NpcTypeId.Monster,
                NpcRaceId = NpcRaceId.Humanoid,
                NpcSexId = NpcSexId.Male
            },
            new Npc
            {
                Id = 1002,
                Level = 10,
                Name = "Goblin Raider",
                NpcTypeId = NpcTypeId.Monster,
                NpcRaceId = NpcRaceId.Humanoid,
                NpcSexId = NpcSexId.Male
            },
            new Npc
            {
                Id = 2000,
                Level = 1,
                Name = null,
                NpcTypeId = NpcTypeId.EffectPoint,
                NpcRaceId = null,
                NpcSexId = NpcSexId.Etc
            },
            new Npc
            {
                Id = 2001,
                Level = 20,
                Name = "Elven Trader",
                NpcTypeId = NpcTypeId.Merchant,
                NpcRaceId = NpcRaceId.Elf,
                NpcSexId = NpcSexId.Female
            });
        await context.SaveChangesAsync();
    }

    private async Task SeedSkillsAsync()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql(database!.ConnectionString)
            .Options;
        await using var context = new GameContentDbContext(options);
        context.AddRange(
            new SkillOperateType { Id = SkillOperateTypeId.A1, Name = "A1" },
            new SkillTargetType { Id = SkillTargetTypeId.One, Name = "ONE" });
        context.Skills.AddRange(
            new Skill
            {
                Id = 1,
                Levels = 37,
                Name = "Triple Slash",
                SkillOperateTypeId = SkillOperateTypeId.A1,
                SkillTargetTypeId = SkillTargetTypeId.One
            },
            new Skill
            {
                Id = 5,
                Levels = 31,
                Name = "Double Sonic Slash",
                SkillOperateTypeId = SkillOperateTypeId.A1,
                SkillTargetTypeId = SkillTargetTypeId.One
            });
        context.SkillIcons.Add(new SkillIcon
        {
            SkillId = 1,
            Level = 1,
            Name = "icon.skill0001"
        });
        await context.SaveChangesAsync();
    }

    private sealed class StudioFactory(string connectionString) : WebApplicationFactory<StudioApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PostgreSql", connectionString);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Dependencies:PostgreSqlRequired"] = "false",
                    ["GameContent:RunMigrations"] = "true",
                    ["GameContent:SeedNpcLookups"] = "true",
                    ["GameContent:SeedPlayerLookups"] = "true",
                    ["GameContent:SeedPlayerClasses"] = "true",
                    ["GameContent:SeedPlayerAppearances"] = "true",
                    ["GameContent:SeedNpcs"] = "false",
                    ["GameContent:SeedSkills"] = "false"
                }));
        }
    }
}

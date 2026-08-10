using L2.Studio.Content;
using L2.Studio.Content.Entities;
using L2.Studio.Content.Identifiers;
using L2.Studio.Content.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace L2.Foundation.Tests;

[Collection(PostgreSqlIntegrationCollection.Name)]
public sealed class GameContentMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlIntegrationFixture postgres;
    private PostgreSqlDatabaseLease? database;

    public GameContentMigrationTests(PostgreSqlIntegrationFixture postgres) => this.postgres = postgres;

    public async Task InitializeAsync() => database = await postgres.CreateDatabaseAsync();

    public async Task DisposeAsync()
    {
        if (database is not null) await database.DisposeAsync();
    }

    [Fact]
    public async Task Initial_migration_creates_the_studio_content_schema()
    {
        await using var contentContext = CreateContentContext();
        await contentContext.Database.MigrateAsync();

        Assert.Empty(await contentContext.Database.GetPendingMigrationsAsync());

        var tables = await contentContext.Database
            .SqlQueryRaw<string>(
                "SELECT table_name AS \"Value\" FROM information_schema.tables " +
                "WHERE table_schema = 'content' AND table_name <> '__EFMigrationsHistory' ORDER BY table_name")
            .ToListAsync();
        Assert.Equal(
            [
                "asset_catalog_groups",
                "asset_catalog_items",
                "asset_catalogs",
                "asset_import_jobs",
                "npc_races",
                "npc_sexes",
                "npc_types",
                "npcs",
                "player_classes",
                "player_faces",
                "player_hair_colors",
                "player_hair_styles",
                "player_races",
                "player_sexes",
                "skill_icons",
                "skill_operate_types",
                "skill_target_types",
                "skills"
            ],
            tables);

        var histories = await contentContext.Database
            .SqlQueryRaw<string>(
                "SELECT table_schema || '.' || table_name AS \"Value\" FROM information_schema.tables " +
                "WHERE table_name = '__EFMigrationsHistory' ORDER BY table_schema")
            .ToListAsync();
        Assert.Equal(["content.__EFMigrationsHistory"], histories);

        var columns = await contentContext.Database
            .SqlQueryRaw<string>(
                "SELECT column_name || ':' || data_type || ':' || is_nullable AS \"Value\" " +
                "FROM information_schema.columns WHERE table_schema = 'content' AND table_name = 'npcs' " +
                "ORDER BY ordinal_position")
            .ToListAsync();
        Assert.Equal(
            [
                "id:integer:NO",
                "level:smallint:NO",
                "name:character varying:YES",
                "npc_type_id:integer:NO",
                "npc_race_id:integer:YES",
                "npc_sex_id:integer:NO"
            ],
            columns);

        var playerClassColumns = await contentContext.Database
            .SqlQueryRaw<string>(
                "SELECT column_name || ':' || data_type || ':' || is_nullable AS \"Value\" " +
                "FROM information_schema.columns WHERE table_schema = 'content' AND table_name = 'player_classes' " +
                "ORDER BY ordinal_position")
            .ToListAsync();
        Assert.Equal(
            [
                "id:integer:NO",
                "player_sex_id:integer:NO",
                "player_race_id:integer:NO",
                "name:character varying:NO",
                "parent_class_id:integer:YES",
                "is_mage:boolean:NO"
            ],
            playerClassColumns);

        var playerClassIndexes = await contentContext.Database
            .SqlQueryRaw<string>(
                "SELECT indexname AS \"Value\" FROM pg_indexes " +
                "WHERE schemaname = 'content' AND tablename = 'player_classes' ORDER BY indexname")
            .ToListAsync();
        Assert.Equal(
            [
                "PK_player_classes",
                "ix_player_classes_name_sex_race",
                "ix_player_classes_parent_sex_race",
                "ix_player_classes_player_race_id",
                "ix_player_classes_player_sex_id"
            ],
            playerClassIndexes);

        var skillColumns = await contentContext.Database
            .SqlQueryRaw<string>(
                "SELECT column_name || ':' || data_type || ':' || is_nullable AS \"Value\" " +
                "FROM information_schema.columns WHERE table_schema = 'content' AND table_name = 'skills' " +
                "ORDER BY ordinal_position")
            .ToListAsync();
        Assert.Equal(
            [
                "id:integer:NO",
                "levels:smallint:NO",
                "name:character varying:NO",
                "skill_operate_type_id:integer:YES",
                "skill_target_type_id:integer:YES"
            ],
            skillColumns);

        var skillIconColumns = await contentContext.Database
            .SqlQueryRaw<string>(
                "SELECT column_name || ':' || data_type || ':' || is_nullable AS \"Value\" " +
                "FROM information_schema.columns WHERE table_schema = 'content' AND table_name = 'skill_icons' " +
                "ORDER BY ordinal_position")
            .ToListAsync();
        Assert.Equal(
            [
                "name:character varying:NO",
                "skill_id:integer:NO",
                "level:smallint:NO"
            ],
            skillIconColumns);

        var indexes = await contentContext.Database
            .SqlQueryRaw<string>(
                "SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname = 'content' " +
                "AND indexname LIKE 'ix_%' ORDER BY indexname")
            .ToListAsync();
        Assert.Equal(
            [
                "ix_asset_catalog_groups_catalog_name",
                "ix_asset_catalog_items_catalog_group_name",
                "ix_asset_catalog_items_catalog_name",
                "ix_asset_catalog_items_catalog_status",
                "ix_asset_catalogs_active_kind",
                "ix_asset_import_jobs_active_kind",
                "ix_asset_import_jobs_claim",
                "ix_npc_races_name",
                "ix_npc_sexes_name",
                "ix_npc_types_name",
                "ix_npcs_npc_race_id",
                "ix_npcs_npc_sex_id",
                "ix_npcs_npc_type_id",
                "ix_player_classes_name_sex_race",
                "ix_player_classes_parent_sex_race",
                "ix_player_classes_player_race_id",
                "ix_player_classes_player_sex_id",
                "ix_player_races_name",
                "ix_player_sexes_name",
                "ix_skill_operate_types_name",
                "ix_skill_target_types_name",
                "ix_skills_skill_operate_type_id",
                "ix_skills_skill_target_type_id"
            ],
            indexes);
        Assert.Empty(await contentContext.NpcTypes.ToListAsync());
        Assert.Empty(await contentContext.NpcRaces.ToListAsync());
        Assert.Empty(await contentContext.NpcSexes.ToListAsync());
        Assert.Empty(await contentContext.PlayerRaces.ToListAsync());
        Assert.Empty(await contentContext.PlayerSexes.ToListAsync());
        Assert.Empty(await contentContext.PlayerClasses.ToListAsync());
        Assert.Empty(await contentContext.SkillIcons.ToListAsync());
        Assert.Empty(await contentContext.SkillOperateTypes.ToListAsync());
        Assert.Empty(await contentContext.SkillTargetTypes.ToListAsync());
        Assert.Empty(await contentContext.Skills.ToListAsync());
        Assert.Empty(await contentContext.AssetImportJobs.ToListAsync());
        Assert.Empty(await contentContext.AssetCatalogs.ToListAsync());
    }

    [Fact]
    public async Task Npc_relationships_enforce_content_integrity()
    {
        await using (var migrationContext = CreateContentContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using (var writeContext = CreateContentContext())
        {
            writeContext.AddRange(
                new NpcType { Id = NpcTypeId.Monster, Name = "Monster" },
                new NpcRace { Id = NpcRaceId.Humanoid, Name = "HUMANOID" },
                new NpcSex { Id = NpcSexId.Male, Name = "MALE" });
            writeContext.Npcs.AddRange(
                CreateNpc(20003, "Goblin"),
                CreateNpc(20004, "Goblin"));
            await writeContext.SaveChangesAsync();
        }

        await using (var readContext = CreateContentContext())
        {
            var npc = await readContext.Npcs
                .AsNoTracking()
                .Include(entity => entity.NpcType)
                .Include(entity => entity.NpcRace)
                .Include(entity => entity.NpcSex)
                .SingleAsync(entity => entity.Id == 20003);

            Assert.Equal("Monster", npc.NpcType.Name);
            Assert.Equal("HUMANOID", npc.NpcRace!.Name);
            Assert.Equal("MALE", npc.NpcSex.Name);
            Assert.Equal(2, await readContext.Npcs.CountAsync(entity => entity.Name == "Goblin"));
        }

        await using (var duplicateLookupContext = CreateContentContext())
        {
            duplicateLookupContext.NpcTypes.Add(new NpcType { Id = NpcTypeId.Artefact, Name = "Monster" });
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateLookupContext.SaveChangesAsync());
        }

        await using (var invalidForeignKeyContext = CreateContentContext())
        {
            invalidForeignKeyContext.Npcs.Add(CreateNpc(20005, "Imp", npcTypeId: (NpcTypeId)999));
            await Assert.ThrowsAsync<DbUpdateException>(() => invalidForeignKeyContext.SaveChangesAsync());
        }

        await using (var invalidLevelContext = CreateContentContext())
        {
            invalidLevelContext.Npcs.Add(CreateNpc(20006, "Invalid", level: 0));
            await Assert.ThrowsAsync<DbUpdateException>(() => invalidLevelContext.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Nullable_race_migration_converts_the_removed_none_race_to_null()
    {
        await using (var previousContext = CreateContentContext())
        {
            await previousContext.GetService<IMigrator>()
                .MigrateAsync("20260805024500_NullableNpcName");
            previousContext.AddRange(
                new NpcType { Id = NpcTypeId.Monster, Name = "Monster" },
                new NpcRace { Id = (NpcRaceId)19, Name = "NONE" },
                new NpcSex { Id = NpcSexId.Male, Name = "MALE" });
            previousContext.Npcs.Add(new Npc
            {
                Id = 20001,
                Level = 1,
                Name = "Gremlin",
                NpcTypeId = NpcTypeId.Monster,
                NpcRaceId = (NpcRaceId)19,
                NpcSexId = NpcSexId.Male
            });
            await previousContext.SaveChangesAsync();
        }

        await using (var migrationContext = CreateContentContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using var verificationContext = CreateContentContext();
        Assert.Null((await verificationContext.Npcs.FindAsync(20001))!.NpcRaceId);
        Assert.Null(await verificationContext.NpcRaces.FindAsync((NpcRaceId)19));
    }

    [Fact]
    public async Task Npc_lookup_seeder_is_repeatable_and_preserves_custom_rows()
    {
        await using (var migrationContext = CreateContentContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        var factory = new TestGameContentDbContextFactory(this);
        var seeder = new NpcLookupSeeder(factory, NullLogger<NpcLookupSeeder>.Instance);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        await using (var seededContext = CreateContentContext())
        {
            Assert.Equal(48, await seededContext.NpcTypes.CountAsync());
            Assert.Equal(22, await seededContext.NpcRaces.CountAsync());
            Assert.Equal(3, await seededContext.NpcSexes.CountAsync());
            Assert.Equal("Folk", (await seededContext.NpcTypes.FindAsync(NpcTypeId.Folk))!.Name);

            (await seededContext.NpcTypes.FindAsync(NpcTypeId.Folk))!.Name = "Incorrect";
            seededContext.NpcTypes.Add(new NpcType { Id = (NpcTypeId)1000, Name = "CustomType" });
            await seededContext.SaveChangesAsync();
        }

        await seeder.SeedAsync();

        await using var verificationContext = CreateContentContext();
        Assert.Equal("Folk", (await verificationContext.NpcTypes.FindAsync(NpcTypeId.Folk))!.Name);
        Assert.Equal("CustomType", (await verificationContext.NpcTypes.FindAsync((NpcTypeId)1000))!.Name);
    }

    [Fact]
    public async Task Player_class_seeder_is_repeatable_updates_source_rows_and_preserves_custom_rows()
    {
        await using (var migrationContext = CreateContentContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        var factory = new TestGameContentDbContextFactory(this);
        var lookupSeeder = new PlayerLookupSeeder(factory, NullLogger<PlayerLookupSeeder>.Instance);
        var seeder = new PlayerClassSeeder(factory, NullLogger<PlayerClassSeeder>.Instance);
        await lookupSeeder.SeedAsync();
        await seeder.SeedAsync();

        await using (var modifiedContext = CreateContentContext())
        {
            var duelist = await modifiedContext.PlayerClasses.SingleAsync(
                entity => entity.Id == PlayerClassId.Duelist &&
                    entity.PlayerRaceId == PlayerRaceId.Human &&
                    entity.PlayerSexId == PlayerSexId.Male);
            duelist.Name = "Modified";
            duelist.ParentClassId = null;
            modifiedContext.PlayerClasses.Add(new PlayerClass
            {
                Id = (PlayerClassId)1000,
                PlayerRaceId = PlayerRaceId.Human,
                PlayerSexId = PlayerSexId.Male,
                Name = "Custom Class"
            });
            await modifiedContext.SaveChangesAsync();
        }

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        await using var verificationContext = CreateContentContext();
        var restoredDuelist = await verificationContext.PlayerClasses
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == PlayerClassId.Duelist &&
                entity.PlayerRaceId == PlayerRaceId.Human &&
                entity.PlayerSexId == PlayerSexId.Male);
        Assert.Equal("Duelist", restoredDuelist.Name);
        Assert.Equal(PlayerClassId.Gladiator, restoredDuelist.ParentClassId);
        Assert.Equal(
            "Custom Class",
            (await verificationContext.PlayerClasses.FindAsync(
                (PlayerClassId)1000,
                PlayerSexId.Male,
                PlayerRaceId.Human))!.Name);
        Assert.Equal(179, await verificationContext.PlayerClasses.CountAsync());
    }

    [Fact]
    public async Task Player_lookup_seeder_is_repeatable_updates_source_rows_and_preserves_custom_rows()
    {
        await using (var migrationContext = CreateContentContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        var factory = new TestGameContentDbContextFactory(this);
        var seeder = new PlayerLookupSeeder(factory, NullLogger<PlayerLookupSeeder>.Instance);
        await seeder.SeedAsync();

        await using (var modifiedContext = CreateContentContext())
        {
            (await modifiedContext.PlayerRaces.FindAsync(PlayerRaceId.Human))!.Name = "Modified";
            modifiedContext.PlayerRaces.Add(new PlayerRace
            {
                Id = (PlayerRaceId)1000,
                Name = "Custom Race"
            });
            await modifiedContext.SaveChangesAsync();
        }

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        await using var verificationContext = CreateContentContext();
        Assert.Equal("Human", (await verificationContext.PlayerRaces.FindAsync(PlayerRaceId.Human))!.Name);
        Assert.Equal(
            "Custom Race",
            (await verificationContext.PlayerRaces.FindAsync((PlayerRaceId)1000))!.Name);
        Assert.Equal(6, await verificationContext.PlayerRaces.CountAsync());
        Assert.Equal(2, await verificationContext.PlayerSexes.CountAsync());
    }

    [Fact]
    public async Task Player_class_variant_migration_replaces_the_interim_table()
    {
        await using (var previousContext = CreateContentContext())
        {
            await previousContext.GetService<IMigrator>().MigrateAsync("20260809171034_AddPlayerClasses");
            await previousContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO content.player_classes (id, name) VALUES (0, 'Human Fighter');");
        }

        await using var migrationContext = CreateContentContext();
        await migrationContext.Database.MigrateAsync();

        Assert.Empty(await migrationContext.PlayerClasses.ToListAsync());
        Assert.Empty(await migrationContext.PlayerRaces.ToListAsync());
        Assert.Empty(await migrationContext.PlayerSexes.ToListAsync());
    }

    [Fact]
    public async Task Skill_icon_migration_replaces_the_old_lookup_shape_for_application_reseeding()
    {
        await using (var previousContext = CreateContentContext())
        {
            await previousContext.GetService<IMigrator>()
                .MigrateAsync("20260805050000_AddSkillClassificationTypes");
            await previousContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO content.skill_icons (id, name) VALUES (1, 'icon.skill0001'); " +
                "INSERT INTO content.skills (id, levels, name, skill_icon_id) " +
                "VALUES (1, 37, 'Triple Slash', 1);");
        }

        await using (var migrationContext = CreateContentContext())
        {
            await migrationContext.Database.MigrateAsync();
            Assert.Empty(await migrationContext.SkillIcons.ToListAsync());
            Assert.NotNull(await migrationContext.Skills.FindAsync(1));
        }

        var factory = new TestGameContentDbContextFactory(this);
        await new SkillSeeder(factory, NullLogger<SkillSeeder>.Instance).SeedAsync();

        await using var verificationContext = CreateContentContext();
        Assert.Equal(
            "icon.skill0001",
            (await verificationContext.SkillIcons.FindAsync(1, (short)1))!.Name);
        Assert.Equal(37, await verificationContext.SkillIcons.CountAsync(icon => icon.SkillId == 1));
    }

    [Fact]
    public async Task Npc_seeder_is_repeatable_updates_source_rows_and_preserves_custom_rows()
    {
        await using (var migrationContext = CreateContentContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        var factory = new TestGameContentDbContextFactory(this);
        await new NpcLookupSeeder(factory, NullLogger<NpcLookupSeeder>.Instance).SeedAsync();
        var seeder = new NpcSeeder(factory, NullLogger<NpcSeeder>.Instance);

        await seeder.SeedAsync();

        await using (var modifiedContext = CreateContentContext())
        {
            var seededNpc = await modifiedContext.Npcs.SingleAsync(entity => entity.Id == 20001);
            seededNpc.Level = 99;
            seededNpc.Name = "Modified";
            seededNpc.NpcTypeId = NpcTypeId.Folk;
            seededNpc.NpcRaceId = NpcRaceId.Human;
            seededNpc.NpcSexId = NpcSexId.Female;
            modifiedContext.Npcs.Add(CreateNpc(90000, "Custom NPC"));
            await modifiedContext.SaveChangesAsync();
        }

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        await using var verificationContext = CreateContentContext();
        var restoredNpc = await verificationContext.Npcs.AsNoTracking().SingleAsync(entity => entity.Id == 20001);
        Assert.Equal(1, restoredNpc.Level);
        Assert.Equal("Gremlin", restoredNpc.Name);
        Assert.Equal(NpcTypeId.Monster, restoredNpc.NpcTypeId);
        Assert.Equal(NpcRaceId.Fairy, restoredNpc.NpcRaceId);
        Assert.Equal(NpcSexId.Male, restoredNpc.NpcSexId);
        Assert.Equal("Custom NPC", (await verificationContext.Npcs.FindAsync(90000))!.Name);
        Assert.Equal(NpcSeedValues.Npcs.Count + 1, await verificationContext.Npcs.CountAsync());
    }

    [Fact]
    public async Task Skill_seeder_is_repeatable_updates_source_rows_and_preserves_custom_rows()
    {
        await using (var migrationContext = CreateContentContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        var factory = new TestGameContentDbContextFactory(this);
        var seeder = new SkillSeeder(factory, NullLogger<SkillSeeder>.Instance);
        await seeder.SeedAsync();

        await using (var modifiedContext = CreateContentContext())
        {
            var seededSkill = await modifiedContext.Skills.SingleAsync(entity => entity.Id == 1);
            var seededIcon = await modifiedContext.SkillIcons.FindAsync(1, (short)1);
            seededSkill.Levels = 1;
            seededSkill.Name = "Modified";
            seededSkill.SkillOperateTypeId = null;
            seededSkill.SkillTargetTypeId = null;
            seededIcon!.Name = "icon.modified";
            modifiedContext.SkillOperateTypes.Add(new SkillOperateType
            {
                Id = (SkillOperateTypeId)10000,
                Name = "CUSTOM_OPERATE"
            });
            modifiedContext.SkillTargetTypes.Add(new SkillTargetType
            {
                Id = (SkillTargetTypeId)10000,
                Name = "CUSTOM_TARGET"
            });
            modifiedContext.Skills.Add(new Skill
            {
                Id = 900000,
                Levels = 1,
                Name = "Custom skill",
                SkillOperateTypeId = (SkillOperateTypeId)10000,
                SkillTargetTypeId = (SkillTargetTypeId)10000
            });
            modifiedContext.SkillIcons.Add(new SkillIcon
            {
                SkillId = 900000,
                Level = 1,
                Name = "icon.custom"
            });
            await modifiedContext.SaveChangesAsync();
        }

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        await using var verificationContext = CreateContentContext();
        var restoredSkill = await verificationContext.Skills
            .AsNoTracking()
            .Include(entity => entity.SkillIcons)
            .Include(entity => entity.SkillOperateType)
            .Include(entity => entity.SkillTargetType)
            .SingleAsync(entity => entity.Id == 1);
        Assert.Equal(37, restoredSkill.Levels);
        Assert.Equal("Triple Slash", restoredSkill.Name);
        Assert.Equal(37, restoredSkill.SkillIcons.Count);
        Assert.Equal(
            Enumerable.Range(1, 37),
            restoredSkill.SkillIcons.OrderBy(icon => icon.Level).Select(icon => (int)icon.Level));
        Assert.All(restoredSkill.SkillIcons, icon => Assert.Equal("icon.skill0001", icon.Name));
        Assert.Equal("A1", restoredSkill.SkillOperateType!.Name);
        Assert.Equal("ONE", restoredSkill.SkillTargetType!.Name);
        Assert.Equal("Custom skill", (await verificationContext.Skills.FindAsync(900000))!.Name);
        Assert.Equal(SkillSeedValues.Icons.Count + 1, await verificationContext.SkillIcons.CountAsync());
        Assert.Equal(
            SkillSeedValues.OperateTypes.Count + 1,
            await verificationContext.SkillOperateTypes.CountAsync());
        Assert.Equal(
            SkillSeedValues.TargetTypes.Count + 1,
            await verificationContext.SkillTargetTypes.CountAsync());
        Assert.Equal(SkillSeedValues.Skills.Count + 1, await verificationContext.Skills.CountAsync());
    }

    private static Npc CreateNpc(
        int id,
        string name,
        NpcTypeId npcTypeId = NpcTypeId.Monster,
        short level = 5) => new()
        {
            Id = id,
            Level = level,
            Name = name,
            NpcTypeId = npcTypeId,
            NpcRaceId = NpcRaceId.Humanoid,
            NpcSexId = NpcSexId.Male
        };

    private GameContentDbContext CreateContentContext()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql(
                database!.ConnectionString,
                configuration => configuration.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    GameContentDbContext.SchemaName))
            .Options;
        return new GameContentDbContext(options);
    }

    private sealed class TestGameContentDbContextFactory(GameContentMigrationTests tests)
        : IDbContextFactory<GameContentDbContext>
    {
        public GameContentDbContext CreateDbContext() => tests.CreateContentContext();
    }
}

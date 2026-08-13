using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class NameKeyedNpcLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_npcs_npc_races_game_version_npc_race_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropForeignKey(
                name: "FK_npcs_npc_sexes_game_version_npc_sex_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropForeignKey(
                name: "FK_npcs_npc_types_game_version_npc_type_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropIndex(
                name: "IX_npcs_game_version_npc_race_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropIndex(
                name: "IX_npcs_game_version_npc_sex_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropIndex(
                name: "IX_npcs_game_version_npc_type_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropIndex(
                name: "ix_npcs_npc_race_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropIndex(
                name: "ix_npcs_npc_sex_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropIndex(
                name: "ix_npcs_npc_type_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_npc_types",
                schema: "content",
                table: "npc_types");

            migrationBuilder.DropIndex(
                name: "ix_npc_types_name",
                schema: "content",
                table: "npc_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_npc_sexes",
                schema: "content",
                table: "npc_sexes");

            migrationBuilder.DropIndex(
                name: "ix_npc_sexes_name",
                schema: "content",
                table: "npc_sexes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_npc_races",
                schema: "content",
                table: "npc_races");

            migrationBuilder.DropIndex(
                name: "ix_npc_races_name",
                schema: "content",
                table: "npc_races");

            migrationBuilder.AddColumn<string>(
                name: "npc_race_name",
                schema: "content",
                table: "npcs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "npc_sex_name",
                schema: "content",
                table: "npcs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "npc_type_name",
                schema: "content",
                table: "npcs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "content",
                table: "npc_types",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "content",
                table: "npc_sexes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "content",
                table: "npc_races",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM content.npc_types WHERE id NOT BETWEEN 1 AND 48) THEN
                        RAISE EXCEPTION 'Unknown legacy NPC type identifier';
                    END IF;
                    IF EXISTS (SELECT 1 FROM content.npc_races WHERE id NOT BETWEEN 0 AND 22 OR id = 19) THEN
                        RAISE EXCEPTION 'Unknown legacy NPC race identifier';
                    END IF;
                    IF EXISTS (SELECT 1 FROM content.npc_sexes WHERE id NOT BETWEEN 0 AND 2) THEN
                        RAISE EXCEPTION 'Unknown legacy NPC sex identifier';
                    END IF;
                END $$;

                UPDATE content.npc_types SET name = (ARRAY[
                    'Adventurer', 'Artefact', 'Auctioneer', 'BabyPet', 'BroadcastingTower',
                    'CastleDoorman', 'Chest', 'ClanHallDoorman', 'ClanHallManager', 'ControlTower',
                    'DawnPriest', 'Defender', 'Doorman', 'DungeonGatekeeper', 'DuskPriest',
                    'EffectPoint', 'EventMonster', 'FeedableBeast', 'FestivalGuide', 'FestivalMonster',
                    'Fisherman', 'FlameTower', 'FlyTerrainObject', 'Folk', 'FriendlyMob', 'GrandBoss',
                    'Guard', 'Merchant', 'Monster', 'OlympiadManager', 'Pet', 'PetManager', 'RaceManager',
                    'RaidBoss', 'RiftInvader', 'SchemeBuffer', 'Servitor', 'SignsPriest', 'TamedBeast',
                    'Teleporter', 'Trainer', 'VillageMasterDElf', 'VillageMasterDwarf',
                    'VillageMasterFighter', 'VillageMasterMystic', 'VillageMasterOrc',
                    'VillageMasterPriest', 'Warehouse'
                ])[id];
                UPDATE content.npc_races SET name = CASE id
                    WHEN 0 THEN 'HUMAN' WHEN 1 THEN 'ELF' WHEN 2 THEN 'DARK_ELF'
                    WHEN 3 THEN 'ORC' WHEN 4 THEN 'DWARF' WHEN 5 THEN 'ANIMAL'
                    WHEN 6 THEN 'BEAST' WHEN 7 THEN 'BUG' WHEN 8 THEN 'CASTLE_GUARD'
                    WHEN 9 THEN 'CONSTRUCT' WHEN 10 THEN 'DEMONIC' WHEN 11 THEN 'DIVINE'
                    WHEN 12 THEN 'DRAGON' WHEN 13 THEN 'ELEMENTAL' WHEN 14 THEN 'ETC'
                    WHEN 15 THEN 'FAIRY' WHEN 16 THEN 'GIANT' WHEN 17 THEN 'HUMANOID'
                    WHEN 18 THEN 'MERCENARY' WHEN 20 THEN 'PLANT' WHEN 21 THEN 'SIEGE_WEAPON'
                    WHEN 22 THEN 'UNDEAD' END;
                UPDATE content.npc_sexes SET name = CASE id
                    WHEN 0 THEN 'MALE' WHEN 1 THEN 'FEMALE' WHEN 2 THEN 'ETC' END;

                UPDATE content.npc_types SET display_name = CASE id
                    WHEN 1 THEN 'Adventurer' WHEN 2 THEN 'Artefact' WHEN 3 THEN 'Auctioneer'
                    WHEN 4 THEN 'Baby Pet' WHEN 5 THEN 'Broadcasting Tower' WHEN 6 THEN 'Castle Doorman'
                    WHEN 7 THEN 'Chest' WHEN 8 THEN 'Clan Hall Doorman' WHEN 9 THEN 'Clan Hall Manager'
                    WHEN 10 THEN 'Control Tower' WHEN 11 THEN 'Dawn Priest' WHEN 12 THEN 'Defender'
                    WHEN 13 THEN 'Doorman' WHEN 14 THEN 'Dungeon Gatekeeper' WHEN 15 THEN 'Dusk Priest'
                    WHEN 16 THEN 'Effect Point' WHEN 17 THEN 'Event Monster' WHEN 18 THEN 'Feedable Beast'
                    WHEN 19 THEN 'Festival Guide' WHEN 20 THEN 'Festival Monster' WHEN 21 THEN 'Fisherman'
                    WHEN 22 THEN 'Flame Tower' WHEN 23 THEN 'Fly Terrain Object' WHEN 24 THEN 'Folk'
                    WHEN 25 THEN 'Friendly Mob' WHEN 26 THEN 'Grand Boss' WHEN 27 THEN 'Guard'
                    WHEN 28 THEN 'Merchant' WHEN 29 THEN 'Monster' WHEN 30 THEN 'Olympiad Manager'
                    WHEN 31 THEN 'Pet' WHEN 32 THEN 'Pet Manager' WHEN 33 THEN 'Race Manager'
                    WHEN 34 THEN 'Raid Boss' WHEN 35 THEN 'Rift Invader' WHEN 36 THEN 'Scheme Buffer'
                    WHEN 37 THEN 'Servitor' WHEN 38 THEN 'Signs Priest' WHEN 39 THEN 'Tamed Beast'
                    WHEN 40 THEN 'Teleporter' WHEN 41 THEN 'Trainer' WHEN 42 THEN 'Village Master Dark Elf'
                    WHEN 43 THEN 'Village Master Dwarf' WHEN 44 THEN 'Village Master Fighter'
                    WHEN 45 THEN 'Village Master Mystic' WHEN 46 THEN 'Village Master Orc'
                    WHEN 47 THEN 'Village Master Priest' WHEN 48 THEN 'Warehouse' END;
                UPDATE content.npc_races SET display_name = initcap(replace(name, '_', ' '));
                UPDATE content.npc_sexes SET display_name = initcap(name);
                UPDATE content.npcs n SET npc_type_name = t.name
                    FROM content.npc_types t WHERE t.game_version = n.game_version AND t.id = n.npc_type_id;
                UPDATE content.npcs n SET npc_race_name = r.name
                    FROM content.npc_races r WHERE r.game_version = n.game_version AND r.id = n.npc_race_id;
                UPDATE content.npcs n SET npc_sex_name = s.name
                    FROM content.npc_sexes s WHERE s.game_version = n.game_version AND s.id = n.npc_sex_id;
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM content.npcs WHERE npc_type_name IS NULL OR npc_sex_name IS NULL) THEN
                        RAISE EXCEPTION 'An NPC references an unknown legacy type or sex identifier';
                    END IF;
                    IF EXISTS (SELECT 1 FROM content.npcs WHERE npc_race_id IS NOT NULL AND npc_race_name IS NULL) THEN
                        RAISE EXCEPTION 'An NPC references an unknown legacy race identifier';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(name: "npc_type_name", schema: "content", table: "npcs",
                type: "character varying(64)", maxLength: 64, nullable: false, oldClrType: typeof(string),
                oldType: "character varying(64)", oldMaxLength: 64, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "npc_sex_name", schema: "content", table: "npcs",
                type: "character varying(64)", maxLength: 64, nullable: false, oldClrType: typeof(string),
                oldType: "character varying(64)", oldMaxLength: 64, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "display_name", schema: "content", table: "npc_types",
                type: "character varying(64)", maxLength: 64, nullable: false, oldClrType: typeof(string),
                oldType: "character varying(64)", oldMaxLength: 64, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "display_name", schema: "content", table: "npc_races",
                type: "character varying(64)", maxLength: 64, nullable: false, oldClrType: typeof(string),
                oldType: "character varying(64)", oldMaxLength: 64, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "display_name", schema: "content", table: "npc_sexes",
                type: "character varying(64)", maxLength: 64, nullable: false, oldClrType: typeof(string),
                oldType: "character varying(64)", oldMaxLength: 64, oldNullable: true);

            migrationBuilder.DropColumn(
                name: "npc_race_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropColumn(
                name: "npc_sex_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropColumn(
                name: "npc_type_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropColumn(
                name: "id",
                schema: "content",
                table: "npc_types");

            migrationBuilder.DropColumn(
                name: "id",
                schema: "content",
                table: "npc_sexes");

            migrationBuilder.DropColumn(
                name: "id",
                schema: "content",
                table: "npc_races");

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_types",
                schema: "content",
                table: "npc_types",
                columns: new[] { "game_version", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_sexes",
                schema: "content",
                table: "npc_sexes",
                columns: new[] { "game_version", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_races",
                schema: "content",
                table: "npc_races",
                columns: new[] { "game_version", "name" });

            migrationBuilder.CreateTable(
                name: "npc_lookup_import_runs",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total_count = table.Column<int>(type: "integer", nullable: false),
                    inserted_count = table.Column<int>(type: "integer", nullable: false),
                    existing_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_lookup_import_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_npc_lookup_import_runs_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_npcs_npc_race_name",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_race_name" });

            migrationBuilder.CreateIndex(
                name: "ix_npcs_npc_sex_name",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_sex_name" });

            migrationBuilder.CreateIndex(
                name: "ix_npcs_npc_type_name",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_type_name" });

            migrationBuilder.CreateIndex(
                name: "ix_npc_lookup_import_runs_active",
                schema: "content",
                table: "npc_lookup_import_runs",
                columns: new[] { "game_version", "kind" },
                unique: true,
                filter: "status IN ('queued', 'running')");

            migrationBuilder.CreateIndex(
                name: "ix_npc_lookup_import_runs_recent",
                schema: "content",
                table: "npc_lookup_import_runs",
                columns: new[] { "game_version", "kind", "requested_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_npc_races_game_version_npc_race_name",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_race_name" },
                principalSchema: "content",
                principalTable: "npc_races",
                principalColumns: new[] { "game_version", "name" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_npc_sexes_game_version_npc_sex_name",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_sex_name" },
                principalSchema: "content",
                principalTable: "npc_sexes",
                principalColumns: new[] { "game_version", "name" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_npc_types_game_version_npc_type_name",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_type_name" },
                principalSchema: "content",
                principalTable: "npc_types",
                principalColumns: new[] { "game_version", "name" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_npcs_npc_races_game_version_npc_race_name",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropForeignKey(
                name: "FK_npcs_npc_sexes_game_version_npc_sex_name",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropForeignKey(
                name: "FK_npcs_npc_types_game_version_npc_type_name",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropTable(
                name: "npc_lookup_import_runs",
                schema: "content");

            migrationBuilder.DropIndex(
                name: "ix_npcs_npc_race_name",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropIndex(
                name: "ix_npcs_npc_sex_name",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropIndex(
                name: "ix_npcs_npc_type_name",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_npc_types",
                schema: "content",
                table: "npc_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_npc_sexes",
                schema: "content",
                table: "npc_sexes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_npc_races",
                schema: "content",
                table: "npc_races");

            migrationBuilder.DropColumn(
                name: "npc_race_name",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropColumn(
                name: "npc_sex_name",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropColumn(
                name: "npc_type_name",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "content",
                table: "npc_types");

            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "content",
                table: "npc_sexes");

            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "content",
                table: "npc_races");

            migrationBuilder.AddColumn<int>(
                name: "npc_race_id",
                schema: "content",
                table: "npcs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "npc_sex_id",
                schema: "content",
                table: "npcs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "npc_type_id",
                schema: "content",
                table: "npcs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "id",
                schema: "content",
                table: "npc_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "id",
                schema: "content",
                table: "npc_sexes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "id",
                schema: "content",
                table: "npc_races",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_types",
                schema: "content",
                table: "npc_types",
                columns: new[] { "game_version", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_sexes",
                schema: "content",
                table: "npc_sexes",
                columns: new[] { "game_version", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_races",
                schema: "content",
                table: "npc_races",
                columns: new[] { "game_version", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_npcs_game_version_npc_race_id",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_race_id" });

            migrationBuilder.CreateIndex(
                name: "IX_npcs_game_version_npc_sex_id",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_sex_id" });

            migrationBuilder.CreateIndex(
                name: "IX_npcs_game_version_npc_type_id",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_npcs_npc_race_id",
                schema: "content",
                table: "npcs",
                column: "npc_race_id");

            migrationBuilder.CreateIndex(
                name: "ix_npcs_npc_sex_id",
                schema: "content",
                table: "npcs",
                column: "npc_sex_id");

            migrationBuilder.CreateIndex(
                name: "ix_npcs_npc_type_id",
                schema: "content",
                table: "npcs",
                column: "npc_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_npc_types_name",
                schema: "content",
                table: "npc_types",
                columns: new[] { "game_version", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_npc_sexes_name",
                schema: "content",
                table: "npc_sexes",
                columns: new[] { "game_version", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_npc_races_name",
                schema: "content",
                table: "npc_races",
                columns: new[] { "game_version", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_npc_races_game_version_npc_race_id",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_race_id" },
                principalSchema: "content",
                principalTable: "npc_races",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_npc_sexes_game_version_npc_sex_id",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_sex_id" },
                principalSchema: "content",
                principalTable: "npc_sexes",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_npc_types_game_version_npc_type_id",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "npc_type_id" },
                principalSchema: "content",
                principalTable: "npc_types",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}

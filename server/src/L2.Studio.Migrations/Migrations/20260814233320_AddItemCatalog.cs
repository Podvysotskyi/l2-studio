using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_actions",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_actions", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_item_actions_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_body_parts",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_body_parts", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_item_body_parts_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_crystal_types",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_crystal_types", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_item_crystal_types_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_import_runs",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "add_missing"),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total_count = table.Column<int>(type: "integer", nullable: false),
                    inserted_count = table.Column<int>(type: "integer", nullable: false),
                    existing_count = table.Column<int>(type: "integer", nullable: false),
                    restored_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_import_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_import_runs_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_materials",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_materials", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_item_materials_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_types",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_types", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_item_types_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    item_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_body_part_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_material_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_crystal_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    icon = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    weapon_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    armor_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    etcitem_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    damage_range = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    display_id = table.Column<int>(type: "integer", nullable: true),
                    crystal_count = table.Column<int>(type: "integer", nullable: true),
                    weight = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<long>(type: "bigint", nullable: true),
                    soulshots = table.Column<int>(type: "integer", nullable: true),
                    spiritshots = table.Column<int>(type: "integer", nullable: true),
                    mp_consume = table.Column<int>(type: "integer", nullable: true),
                    reduced_mp_consume = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reuse_delay = table.Column<int>(type: "integer", nullable: true),
                    recipe_id = table.Column<int>(type: "integer", nullable: true),
                    handler = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_skill = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    use_condition = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    element_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    enchant_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    for_npc = table.Column<bool>(type: "boolean", nullable: true),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_attack_weapon = table.Column<bool>(type: "boolean", nullable: true),
                    is_force_equip = table.Column<bool>(type: "boolean", nullable: true),
                    is_depositable = table.Column<bool>(type: "boolean", nullable: true),
                    is_destroyable = table.Column<bool>(type: "boolean", nullable: true),
                    is_dropable = table.Column<bool>(type: "boolean", nullable: true),
                    is_magic_weapon = table.Column<bool>(type: "boolean", nullable: true),
                    is_oly_restricted = table.Column<bool>(type: "boolean", nullable: true),
                    is_questitem = table.Column<bool>(type: "boolean", nullable: true),
                    is_sellable = table.Column<bool>(type: "boolean", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: true),
                    is_tradable = table.Column<bool>(type: "boolean", nullable: true),
                    use_weapon_skills_only = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => new { x.game_version, x.id });
                    table.ForeignKey(
                        name: "FK_items_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_item_body_parts_game_version_item_body_part_name",
                        columns: x => new { x.game_version, x.item_body_part_name },
                        principalSchema: "content",
                        principalTable: "item_body_parts",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_item_crystal_types_game_version_item_crystal_type_name",
                        columns: x => new { x.game_version, x.item_crystal_type_name },
                        principalSchema: "content",
                        principalTable: "item_crystal_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_item_materials_game_version_item_material_name",
                        columns: x => new { x.game_version, x.item_material_name },
                        principalSchema: "content",
                        principalTable: "item_materials",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_item_types_game_version_item_type_name",
                        columns: x => new { x.game_version, x.item_type_name },
                        principalSchema: "content",
                        principalTable: "item_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_stats",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    accuracy_combat = table.Column<decimal>(type: "numeric", nullable: true),
                    critical_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    magical_attack = table.Column<decimal>(type: "numeric", nullable: true),
                    magical_defence = table.Column<decimal>(type: "numeric", nullable: true),
                    maximum_mp = table.Column<decimal>(type: "numeric", nullable: true),
                    physical_attack = table.Column<decimal>(type: "numeric", nullable: true),
                    physical_attack_range = table.Column<decimal>(type: "numeric", nullable: true),
                    physical_attack_speed = table.Column<decimal>(type: "numeric", nullable: true),
                    physical_defence = table.Column<decimal>(type: "numeric", nullable: true),
                    evasion = table.Column<decimal>(type: "numeric", nullable: true),
                    shield_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    random_damage = table.Column<decimal>(type: "numeric", nullable: true),
                    shield_defence = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_stats", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_stats_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_stats_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_import_runs_active",
                schema: "content",
                table: "item_import_runs",
                column: "game_version",
                unique: true,
                filter: "status IN ('queued', 'running')");

            migrationBuilder.CreateIndex(
                name: "ix_item_import_runs_recent",
                schema: "content",
                table: "item_import_runs",
                columns: new[] { "game_version", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "IX_items_game_version_item_action_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_items_game_version_item_body_part_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_body_part_name" });

            migrationBuilder.CreateIndex(
                name: "IX_items_game_version_item_crystal_type_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_crystal_type_name" });

            migrationBuilder.CreateIndex(
                name: "IX_items_game_version_item_material_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_material_name" });

            migrationBuilder.CreateIndex(
                name: "ix_items_item_type_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_type_name" });

            migrationBuilder.CreateIndex(
                name: "ix_items_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_import_runs",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_stats",
                schema: "content");

            migrationBuilder.DropTable(
                name: "items",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_actions",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_body_parts",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_crystal_types",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_materials",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_types",
                schema: "content");
        }
    }
}

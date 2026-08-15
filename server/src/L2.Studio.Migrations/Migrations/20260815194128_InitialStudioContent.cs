using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialStudioContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content");

            migrationBuilder.CreateTable(
                name: "game_versions",
                schema: "content",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_folder = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_versions", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "asset_catalogs",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_folder = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    source_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    protocol = table.Column<int>(type: "integer", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_catalogs", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_catalogs_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_jobs",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    trigger_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    requested_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_requested_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    force = table.Column<bool>(type: "boolean", nullable: true),
                    discovery_finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    discovered_file_count = table.Column<int>(type: "integer", nullable: true),
                    completed_file_count = table.Column<int>(type: "integer", nullable: true),
                    succeeded_file_count = table.Column<int>(type: "integer", nullable: true),
                    warning_file_count = table.Column<int>(type: "integer", nullable: true),
                    failed_file_count = table.Column<int>(type: "integer", nullable: true),
                    reused_file_count = table.Column<int>(type: "integer", nullable: true),
                    concurrency_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, defaultValue: "add_missing"),
                    total_count = table.Column<int>(type: "integer", nullable: true),
                    inserted_count = table.Column<int>(type: "integer", nullable: true),
                    existing_count = table.Column<int>(type: "integer", nullable: true),
                    restored_count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_import_jobs_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "npc_races",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_races", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_npc_races_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "npc_sexes",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_sexes", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_npc_sexes_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "npc_types",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_types", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_npc_types_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_races",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_races", x => new { x.game_version, x.id });
                    table.ForeignKey(
                        name: "FK_player_races_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_sexes",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_sexes", x => new { x.game_version, x.id });
                    table.ForeignKey(
                        name: "FK_player_sexes_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "skill_operate_types",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_operate_types", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_skill_operate_types_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "skill_target_types",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_target_types", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_skill_target_types_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_import_work_items",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    source_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    source_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    artifact_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total_resource_count = table.Column<int>(type: "integer", nullable: false),
                    processed_resource_count = table.Column<int>(type: "integer", nullable: false),
                    skipped_resource_count = table.Column<int>(type: "integer", nullable: false),
                    warning_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    unpublished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_import_work_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_import_work_items_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_import_work_items_import_jobs_run_id",
                        column: x => x.run_id,
                        principalSchema: "content",
                        principalTable: "import_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "npcs",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    appearance_id = table.Column<int>(type: "integer", nullable: true),
                    level = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    npc_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    npc_race_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    npc_sex_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npcs", x => new { x.game_version, x.id });
                    table.CheckConstraint("ck_npcs_level", "level BETWEEN 1 AND 255");
                    table.ForeignKey(
                        name: "FK_npcs_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npcs_npc_races_game_version_npc_race_name",
                        columns: x => new { x.game_version, x.npc_race_name },
                        principalSchema: "content",
                        principalTable: "npc_races",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npcs_npc_sexes_game_version_npc_sex_name",
                        columns: x => new { x.game_version, x.npc_sex_name },
                        principalSchema: "content",
                        principalTable: "npc_sexes",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npcs_npc_types_game_version_npc_type_name",
                        columns: x => new { x.game_version, x.npc_type_name },
                        principalSchema: "content",
                        principalTable: "npc_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_classes",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    player_sex_id = table.Column<int>(type: "integer", nullable: false),
                    player_race_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_mage = table.Column<bool>(type: "boolean", nullable: false),
                    parent_class_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_classes", x => new { x.game_version, x.id, x.player_sex_id, x.player_race_id });
                    table.ForeignKey(
                        name: "FK_player_classes_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_classes_player_classes_game_version_parent_class_id_~",
                        columns: x => new { x.game_version, x.parent_class_id, x.player_sex_id, x.player_race_id },
                        principalSchema: "content",
                        principalTable: "player_classes",
                        principalColumns: new[] { "game_version", "id", "player_sex_id", "player_race_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_classes_player_races_game_version_player_race_id",
                        columns: x => new { x.game_version, x.player_race_id },
                        principalSchema: "content",
                        principalTable: "player_races",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_classes_player_sexes_game_version_player_sex_id",
                        columns: x => new { x.game_version, x.player_sex_id },
                        principalSchema: "content",
                        principalTable: "player_sexes",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_faces",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    player_sex_id = table.Column<int>(type: "integer", nullable: false),
                    player_race_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_faces", x => new { x.game_version, x.id, x.player_sex_id, x.player_race_id });
                    table.ForeignKey(
                        name: "FK_player_faces_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_faces_player_races_game_version_player_race_id",
                        columns: x => new { x.game_version, x.player_race_id },
                        principalSchema: "content",
                        principalTable: "player_races",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_faces_player_sexes_game_version_player_sex_id",
                        columns: x => new { x.game_version, x.player_sex_id },
                        principalSchema: "content",
                        principalTable: "player_sexes",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_hair_colors",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    player_sex_id = table.Column<int>(type: "integer", nullable: false),
                    player_race_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_hair_colors", x => new { x.game_version, x.id, x.player_sex_id, x.player_race_id });
                    table.ForeignKey(
                        name: "FK_player_hair_colors_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_hair_colors_player_races_game_version_player_race_id",
                        columns: x => new { x.game_version, x.player_race_id },
                        principalSchema: "content",
                        principalTable: "player_races",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_hair_colors_player_sexes_game_version_player_sex_id",
                        columns: x => new { x.game_version, x.player_sex_id },
                        principalSchema: "content",
                        principalTable: "player_sexes",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_hair_styles",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    player_sex_id = table.Column<int>(type: "integer", nullable: false),
                    player_race_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_hair_styles", x => new { x.game_version, x.id, x.player_sex_id, x.player_race_id });
                    table.ForeignKey(
                        name: "FK_player_hair_styles_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_hair_styles_player_races_game_version_player_race_id",
                        columns: x => new { x.game_version, x.player_race_id },
                        principalSchema: "content",
                        principalTable: "player_races",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_hair_styles_player_sexes_game_version_player_sex_id",
                        columns: x => new { x.game_version, x.player_sex_id },
                        principalSchema: "content",
                        principalTable: "player_sexes",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    levels = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    skill_operate_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    skill_target_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => new { x.game_version, x.id });
                    table.CheckConstraint("ck_skills_levels", "levels BETWEEN 1 AND 255");
                    table.ForeignKey(
                        name: "FK_skills_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_skills_skill_operate_types_game_version_skill_operate_type_~",
                        columns: x => new { x.game_version, x.skill_operate_type_name },
                        principalSchema: "content",
                        principalTable: "skill_operate_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_skills_skill_target_types_game_version_skill_target_type_na~",
                        columns: x => new { x.game_version, x.skill_target_type_name },
                        principalSchema: "content",
                        principalTable: "skill_target_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_artifacts",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    source_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipe_version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    build_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    output_root = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    protocol = table.Column<int>(type: "integer", nullable: true),
                    file_count = table.Column<int>(type: "integer", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    integrity_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    publishing_work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_artifacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_artifacts_asset_import_work_items_publishing_work_ite~",
                        column: x => x.publishing_work_item_id,
                        principalSchema: "content",
                        principalTable: "asset_import_work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_artifacts_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_import_diagnostics",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    stage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    object_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_import_diagnostics", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_import_diagnostics_asset_import_work_items_work_item_~",
                        column: x => x.work_item_id,
                        principalSchema: "content",
                        principalTable: "asset_import_work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_asset_import_diagnostics_import_jobs_run_id",
                        column: x => x.run_id,
                        principalSchema: "content",
                        principalTable: "import_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "npc_stats",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_id = table.Column<int>(type: "integer", nullable: false),
                    str = table.Column<int>(type: "integer", nullable: true),
                    @int = table.Column<int>(name: "int", type: "integer", nullable: true),
                    dex = table.Column<int>(type: "integer", nullable: true),
                    wit = table.Column<int>(type: "integer", nullable: true),
                    con = table.Column<int>(type: "integer", nullable: true),
                    men = table.Column<int>(type: "integer", nullable: true),
                    hit_time = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_stats", x => new { x.game_version, x.npc_id });
                    table.ForeignKey(
                        name: "FK_npc_stats_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_stats_npcs_game_version_npc_id",
                        columns: x => new { x.game_version, x.npc_id },
                        principalSchema: "content",
                        principalTable: "npcs",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_stats_attack",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_id = table.Column<int>(type: "integer", nullable: false),
                    physical = table.Column<decimal>(type: "numeric", nullable: true),
                    magical = table.Column<decimal>(type: "numeric", nullable: true),
                    random = table.Column<int>(type: "integer", nullable: true),
                    critical = table.Column<int>(type: "integer", nullable: true),
                    accuracy = table.Column<decimal>(type: "numeric", nullable: true),
                    attack_speed = table.Column<int>(type: "integer", nullable: true),
                    reuse_delay = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    range = table.Column<int>(type: "integer", nullable: true),
                    distance = table.Column<int>(type: "integer", nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_stats_attack", x => new { x.game_version, x.npc_id });
                    table.ForeignKey(
                        name: "FK_npc_stats_attack_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_stats_attack_npcs_game_version_npc_id",
                        columns: x => new { x.game_version, x.npc_id },
                        principalSchema: "content",
                        principalTable: "npcs",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_stats_defence",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_id = table.Column<int>(type: "integer", nullable: false),
                    physical = table.Column<decimal>(type: "numeric", nullable: true),
                    magical = table.Column<decimal>(type: "numeric", nullable: true),
                    evasion = table.Column<int>(type: "integer", nullable: true),
                    shield = table.Column<int>(type: "integer", nullable: true),
                    shield_rate = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_stats_defence", x => new { x.game_version, x.npc_id });
                    table.ForeignKey(
                        name: "FK_npc_stats_defence_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_stats_defence_npcs_game_version_npc_id",
                        columns: x => new { x.game_version, x.npc_id },
                        principalSchema: "content",
                        principalTable: "npcs",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_stats_speed",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_id = table.Column<int>(type: "integer", nullable: false),
                    walk_ground = table.Column<decimal>(type: "numeric", nullable: true),
                    run_ground = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_stats_speed", x => new { x.game_version, x.npc_id });
                    table.ForeignKey(
                        name: "FK_npc_stats_speed_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_stats_speed_npcs_game_version_npc_id",
                        columns: x => new { x.game_version, x.npc_id },
                        principalSchema: "content",
                        principalTable: "npcs",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_stats_vitals",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_id = table.Column<int>(type: "integer", nullable: false),
                    hp = table.Column<decimal>(type: "numeric", nullable: true),
                    hp_regen = table.Column<decimal>(type: "numeric", nullable: true),
                    mp = table.Column<decimal>(type: "numeric", nullable: true),
                    mp_regen = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_stats_vitals", x => new { x.game_version, x.npc_id });
                    table.ForeignKey(
                        name: "FK_npc_stats_vitals_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_stats_vitals_npcs_game_version_npc_id",
                        columns: x => new { x.game_version, x.npc_id },
                        principalSchema: "content",
                        principalTable: "npcs",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_statuses",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_id = table.Column<int>(type: "integer", nullable: false),
                    attackable = table.Column<bool>(type: "boolean", nullable: false),
                    targetable = table.Column<bool>(type: "boolean", nullable: false),
                    talkable = table.Column<bool>(type: "boolean", nullable: false),
                    undying = table.Column<bool>(type: "boolean", nullable: false),
                    show_name = table.Column<bool>(type: "boolean", nullable: false),
                    random_walk = table.Column<bool>(type: "boolean", nullable: false),
                    can_move = table.Column<bool>(type: "boolean", nullable: false),
                    no_sleep_mode = table.Column<bool>(type: "boolean", nullable: false),
                    can_be_sown = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_statuses", x => new { x.game_version, x.npc_id });
                    table.ForeignKey(
                        name: "FK_npc_statuses_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_statuses_npcs_game_version_npc_id",
                        columns: x => new { x.game_version, x.npc_id },
                        principalSchema: "content",
                        principalTable: "npcs",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_icons",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    skill_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_icons", x => new { x.game_version, x.skill_id, x.level });
                    table.CheckConstraint("ck_skill_icons_level", "level BETWEEN 1 AND 255");
                    table.ForeignKey(
                        name: "FK_skill_icons_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_skill_icons_skills_game_version_skill_id",
                        columns: x => new { x.game_version, x.skill_id },
                        principalSchema: "content",
                        principalTable: "skills",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_artifact_dependencies",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    artifact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    dependency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    resolved_artifact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    build_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_artifact_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_artifact_dependencies_asset_artifacts_artifact_id",
                        column: x => x.artifact_id,
                        principalSchema: "content",
                        principalTable: "asset_artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_asset_artifact_dependencies_asset_artifacts_resolved_artifa~",
                        column: x => x.resolved_artifact_id,
                        principalSchema: "content",
                        principalTable: "asset_artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_artifact_files",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    artifact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relative_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    public_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    media_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_artifact_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_artifact_files_asset_artifacts_artifact_id",
                        column: x => x.artifact_id,
                        principalSchema: "content",
                        principalTable: "asset_artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_catalog_sources",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artifact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publishing_work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    source_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    artifact_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    output_root = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    referenced_output_roots_json = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_stale = table.Column<bool>(type: "boolean", nullable: false),
                    stale_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    stale_reasons_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_catalog_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_catalog_sources_asset_artifacts_artifact_id",
                        column: x => x.artifact_id,
                        principalSchema: "content",
                        principalTable: "asset_artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_catalog_sources_asset_catalogs_catalog_id",
                        column: x => x.catalog_id,
                        principalSchema: "content",
                        principalTable: "asset_catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_releases",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    snapshot_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    validation_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    validation_issues_json = table.Column<string>(type: "jsonb", nullable: false),
                    validated_snapshot_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    validation_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    manifest_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    manifest_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    login_scene_file_id = table.Column<long>(type: "bigint", nullable: true),
                    login_camera_sequence = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    login_music_file_id = table.Column<long>(type: "bigint", nullable: true),
                    primary_logo_file_id = table.Column<long>(type: "bigint", nullable: true),
                    version_logo_file_id = table.Column<long>(type: "bigint", nullable: true),
                    loading_artwork_file_id = table.Column<long>(type: "bigint", nullable: true),
                    character_selection_scene_file_id = table.Column<long>(type: "bigint", nullable: true),
                    character_selection_camera_sequence = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_releases", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_releases_asset_artifact_files_character_selection_sce~",
                        column: x => x.character_selection_scene_file_id,
                        principalSchema: "content",
                        principalTable: "asset_artifact_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_releases_asset_artifact_files_loading_artwork_file_id",
                        column: x => x.loading_artwork_file_id,
                        principalSchema: "content",
                        principalTable: "asset_artifact_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_releases_asset_artifact_files_login_music_file_id",
                        column: x => x.login_music_file_id,
                        principalSchema: "content",
                        principalTable: "asset_artifact_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_releases_asset_artifact_files_login_scene_file_id",
                        column: x => x.login_scene_file_id,
                        principalSchema: "content",
                        principalTable: "asset_artifact_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_releases_asset_artifact_files_primary_logo_file_id",
                        column: x => x.primary_logo_file_id,
                        principalSchema: "content",
                        principalTable: "asset_artifact_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_releases_asset_artifact_files_version_logo_file_id",
                        column: x => x.version_logo_file_id,
                        principalSchema: "content",
                        principalTable: "asset_artifact_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_releases_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_catalog_groups",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_catalog_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_catalog_groups_asset_catalog_sources_source_id",
                        column: x => x.source_id,
                        principalSchema: "content",
                        principalTable: "asset_catalog_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_asset_catalog_groups_asset_catalogs_catalog_id",
                        column: x => x.catalog_id,
                        principalSchema: "content",
                        principalTable: "asset_catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_catalog_items",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    group_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_catalog_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_catalog_items_asset_catalog_sources_source_id",
                        column: x => x.source_id,
                        principalSchema: "content",
                        principalTable: "asset_catalog_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_asset_catalog_items_asset_catalogs_catalog_id",
                        column: x => x.catalog_id,
                        principalSchema: "content",
                        principalTable: "asset_catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_catalog_source_dependencies",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    dependency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    resolved_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    artifact_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_catalog_source_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_catalog_source_dependencies_asset_catalog_sources_sou~",
                        column: x => x.source_id,
                        principalSchema: "content",
                        principalTable: "asset_catalog_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_release_artifacts",
                schema: "content",
                columns: table => new
                {
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artifact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_root = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_release_artifacts", x => new { x.release_id, x.artifact_id });
                    table.ForeignKey(
                        name: "FK_asset_release_artifacts_asset_artifacts_artifact_id",
                        column: x => x.artifact_id,
                        principalSchema: "content",
                        principalTable: "asset_artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_release_artifacts_asset_releases_release_id",
                        column: x => x.release_id,
                        principalSchema: "content",
                        principalTable: "asset_releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_release_events",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    details_json = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_release_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_release_events_asset_releases_release_id",
                        column: x => x.release_id,
                        principalSchema: "content",
                        principalTable: "asset_releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_release_pointers",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    desired_release_id = table.Column<Guid>(type: "uuid", nullable: true),
                    published_release_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_release_pointers", x => x.game_version);
                    table.ForeignKey(
                        name: "FK_asset_release_pointers_asset_releases_desired_release_id",
                        column: x => x.desired_release_id,
                        principalSchema: "content",
                        principalTable: "asset_releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_release_pointers_asset_releases_published_release_id",
                        column: x => x.published_release_id,
                        principalSchema: "content",
                        principalTable: "asset_releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_release_pointers_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_artifact_dependencies_key",
                schema: "content",
                table: "asset_artifact_dependencies",
                columns: new[] { "artifact_id", "kind", "dependency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_artifact_dependencies_resolved",
                schema: "content",
                table: "asset_artifact_dependencies",
                column: "resolved_artifact_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_artifact_files_path",
                schema: "content",
                table: "asset_artifact_files",
                columns: new[] { "artifact_id", "relative_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_artifacts_build",
                schema: "content",
                table: "asset_artifacts",
                columns: new[] { "game_version", "kind", "normalized_source_key", "build_fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_artifacts_integrity",
                schema: "content",
                table: "asset_artifacts",
                columns: new[] { "game_version", "kind", "integrity_status" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_artifacts_output_root",
                schema: "content",
                table: "asset_artifacts",
                column: "output_root",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asset_artifacts_publishing_work_item_id",
                schema: "content",
                table: "asset_artifacts",
                column: "publishing_work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalog_groups_catalog_name",
                schema: "content",
                table: "asset_catalog_groups",
                columns: new[] { "catalog_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_asset_catalog_groups_source_id",
                schema: "content",
                table: "asset_catalog_groups",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalog_items_catalog_group_name",
                schema: "content",
                table: "asset_catalog_items",
                columns: new[] { "catalog_id", "group_name", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalog_items_catalog_name",
                schema: "content",
                table: "asset_catalog_items",
                columns: new[] { "catalog_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalog_items_catalog_status",
                schema: "content",
                table: "asset_catalog_items",
                columns: new[] { "catalog_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_asset_catalog_items_source_id",
                schema: "content",
                table: "asset_catalog_items",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalog_source_dependencies_key",
                schema: "content",
                table: "asset_catalog_source_dependencies",
                columns: new[] { "kind", "dependency_key" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalog_source_dependencies_source",
                schema: "content",
                table: "asset_catalog_source_dependencies",
                columns: new[] { "kind", "resolved_source_key" });

            migrationBuilder.CreateIndex(
                name: "IX_asset_catalog_source_dependencies_source_id",
                schema: "content",
                table: "asset_catalog_source_dependencies",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_catalog_sources_artifact_id",
                schema: "content",
                table: "asset_catalog_sources",
                column: "artifact_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalog_sources_catalog_source",
                schema: "content",
                table: "asset_catalog_sources",
                columns: new[] { "catalog_id", "normalized_source_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalogs_active_kind",
                schema: "content",
                table: "asset_catalogs",
                columns: new[] { "game_version", "kind" },
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_diagnostics_filters",
                schema: "content",
                table: "asset_import_diagnostics",
                columns: new[] { "run_id", "severity", "code", "stage" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_diagnostics_source_key",
                schema: "content",
                table: "asset_import_diagnostics",
                column: "source_key");

            migrationBuilder.CreateIndex(
                name: "IX_asset_import_diagnostics_work_item_id",
                schema: "content",
                table: "asset_import_diagnostics",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_import_work_items_game_version",
                schema: "content",
                table: "asset_import_work_items",
                column: "game_version");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_work_items_run_source",
                schema: "content",
                table: "asset_import_work_items",
                columns: new[] { "run_id", "normalized_source_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_work_items_run_status",
                schema: "content",
                table: "asset_import_work_items",
                columns: new[] { "run_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_release_artifacts_artifact",
                schema: "content",
                table: "asset_release_artifacts",
                column: "artifact_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_release_events_release_id",
                schema: "content",
                table: "asset_release_events",
                column: "release_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_release_pointers_desired_release_id",
                schema: "content",
                table: "asset_release_pointers",
                column: "desired_release_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_release_pointers_published_release_id",
                schema: "content",
                table: "asset_release_pointers",
                column: "published_release_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_releases_character_selection_scene_file_id",
                schema: "content",
                table: "asset_releases",
                column: "character_selection_scene_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_releases_loading_artwork_file_id",
                schema: "content",
                table: "asset_releases",
                column: "loading_artwork_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_releases_login_music_file_id",
                schema: "content",
                table: "asset_releases",
                column: "login_music_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_releases_login_scene_file_id",
                schema: "content",
                table: "asset_releases",
                column: "login_scene_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_releases_primary_logo_file_id",
                schema: "content",
                table: "asset_releases",
                column: "primary_logo_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_releases_version_logo_file_id",
                schema: "content",
                table: "asset_releases",
                column: "version_logo_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_releases_version_name",
                schema: "content",
                table: "asset_releases",
                columns: new[] { "game_version", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_releases_version_status",
                schema: "content",
                table: "asset_releases",
                columns: new[] { "game_version", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_game_versions_display_name",
                schema: "content",
                table: "game_versions",
                column: "display_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_runs_active_full_scan_kind",
                schema: "content",
                table: "import_jobs",
                columns: new[] { "game_version", "kind" },
                unique: true,
                filter: "trigger_type = 'full_scan' AND status IN ('queued', 'discovering', 'running')");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_runs_active_single_source",
                schema: "content",
                table: "import_jobs",
                columns: new[] { "game_version", "kind", "normalized_requested_source_key" },
                unique: true,
                filter: "trigger_type = 'single_file' AND status IN ('queued', 'discovering', 'running')");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_runs_kind_requested",
                schema: "content",
                table: "import_jobs",
                columns: new[] { "game_version", "kind", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_active_content_target",
                schema: "content",
                table: "import_jobs",
                columns: new[] { "game_version", "concurrency_key" },
                unique: true,
                filter: "category = 'content' AND status IN ('queued', 'running')");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_recent",
                schema: "content",
                table: "import_jobs",
                columns: new[] { "game_version", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_target_recent",
                schema: "content",
                table: "import_jobs",
                columns: new[] { "game_version", "category", "kind", "requested_at" });

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
                name: "IX_player_classes_game_version_parent_class_id_player_sex_id_p~",
                schema: "content",
                table: "player_classes",
                columns: new[] { "game_version", "parent_class_id", "player_sex_id", "player_race_id" });

            migrationBuilder.CreateIndex(
                name: "IX_player_classes_game_version_player_race_id",
                schema: "content",
                table: "player_classes",
                columns: new[] { "game_version", "player_race_id" });

            migrationBuilder.CreateIndex(
                name: "IX_player_classes_game_version_player_sex_id",
                schema: "content",
                table: "player_classes",
                columns: new[] { "game_version", "player_sex_id" });

            migrationBuilder.CreateIndex(
                name: "ix_player_classes_name_sex_race",
                schema: "content",
                table: "player_classes",
                columns: new[] { "game_version", "name", "player_sex_id", "player_race_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_classes_parent_sex_race",
                schema: "content",
                table: "player_classes",
                columns: new[] { "parent_class_id", "player_sex_id", "player_race_id" });

            migrationBuilder.CreateIndex(
                name: "ix_player_classes_player_race_id",
                schema: "content",
                table: "player_classes",
                column: "player_race_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_classes_player_sex_id",
                schema: "content",
                table: "player_classes",
                column: "player_sex_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_faces_game_version_player_race_id",
                schema: "content",
                table: "player_faces",
                columns: new[] { "game_version", "player_race_id" });

            migrationBuilder.CreateIndex(
                name: "IX_player_faces_game_version_player_sex_id",
                schema: "content",
                table: "player_faces",
                columns: new[] { "game_version", "player_sex_id" });

            migrationBuilder.CreateIndex(
                name: "IX_player_hair_colors_game_version_player_race_id",
                schema: "content",
                table: "player_hair_colors",
                columns: new[] { "game_version", "player_race_id" });

            migrationBuilder.CreateIndex(
                name: "IX_player_hair_colors_game_version_player_sex_id",
                schema: "content",
                table: "player_hair_colors",
                columns: new[] { "game_version", "player_sex_id" });

            migrationBuilder.CreateIndex(
                name: "IX_player_hair_styles_game_version_player_race_id",
                schema: "content",
                table: "player_hair_styles",
                columns: new[] { "game_version", "player_race_id" });

            migrationBuilder.CreateIndex(
                name: "IX_player_hair_styles_game_version_player_sex_id",
                schema: "content",
                table: "player_hair_styles",
                columns: new[] { "game_version", "player_sex_id" });

            migrationBuilder.CreateIndex(
                name: "ix_player_races_name",
                schema: "content",
                table: "player_races",
                columns: new[] { "game_version", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_sexes_name",
                schema: "content",
                table: "player_sexes",
                columns: new[] { "game_version", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skills_skill_operate_type_name",
                schema: "content",
                table: "skills",
                columns: new[] { "game_version", "skill_operate_type_name" });

            migrationBuilder.CreateIndex(
                name: "ix_skills_skill_target_type_name",
                schema: "content",
                table: "skills",
                columns: new[] { "game_version", "skill_target_type_name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_artifact_dependencies",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_catalog_groups",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_catalog_items",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_catalog_source_dependencies",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_import_diagnostics",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_release_artifacts",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_release_events",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_release_pointers",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_stats",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_stats",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_stats_attack",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_stats_defence",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_stats_speed",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_stats_vitals",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_statuses",
                schema: "content");

            migrationBuilder.DropTable(
                name: "player_classes",
                schema: "content");

            migrationBuilder.DropTable(
                name: "player_faces",
                schema: "content");

            migrationBuilder.DropTable(
                name: "player_hair_colors",
                schema: "content");

            migrationBuilder.DropTable(
                name: "player_hair_styles",
                schema: "content");

            migrationBuilder.DropTable(
                name: "skill_icons",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_catalog_sources",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_releases",
                schema: "content");

            migrationBuilder.DropTable(
                name: "items",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npcs",
                schema: "content");

            migrationBuilder.DropTable(
                name: "player_races",
                schema: "content");

            migrationBuilder.DropTable(
                name: "player_sexes",
                schema: "content");

            migrationBuilder.DropTable(
                name: "skills",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_catalogs",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_artifact_files",
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

            migrationBuilder.DropTable(
                name: "npc_races",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_sexes",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_types",
                schema: "content");

            migrationBuilder.DropTable(
                name: "skill_operate_types",
                schema: "content");

            migrationBuilder.DropTable(
                name: "skill_target_types",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_artifacts",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_import_work_items",
                schema: "content");

            migrationBuilder.DropTable(
                name: "import_jobs",
                schema: "content");

            migrationBuilder.DropTable(
                name: "game_versions",
                schema: "content");
        }
    }
}

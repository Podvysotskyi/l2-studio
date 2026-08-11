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
                name: "asset_catalogs",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "asset_import_runs",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trigger_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_requested_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    discovery_finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    discovered_file_count = table.Column<int>(type: "integer", nullable: false),
                    completed_file_count = table.Column<int>(type: "integer", nullable: false),
                    succeeded_file_count = table.Column<int>(type: "integer", nullable: false),
                    warning_file_count = table.Column<int>(type: "integer", nullable: false),
                    failed_file_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_import_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "npc_races",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_races", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "npc_sexes",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_sexes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "npc_types",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_races",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_races", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_sexes",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_sexes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skill_operate_types",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_operate_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skill_target_types",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_target_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asset_catalog_sources",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publishing_work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    source_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    output_root = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    referenced_output_roots_json = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_catalog_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_catalog_sources_asset_catalogs_catalog_id",
                        column: x => x.catalog_id,
                        principalSchema: "content",
                        principalTable: "asset_catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_import_work_items",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_source_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    source_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    source_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
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
                    unpublished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_import_work_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_import_work_items_asset_import_runs_run_id",
                        column: x => x.run_id,
                        principalSchema: "content",
                        principalTable: "asset_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npcs",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    npc_type_id = table.Column<int>(type: "integer", nullable: false),
                    npc_race_id = table.Column<int>(type: "integer", nullable: true),
                    npc_sex_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npcs", x => x.id);
                    table.CheckConstraint("ck_npcs_level", "level BETWEEN 1 AND 255");
                    table.ForeignKey(
                        name: "FK_npcs_npc_races_npc_race_id",
                        column: x => x.npc_race_id,
                        principalSchema: "content",
                        principalTable: "npc_races",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npcs_npc_sexes_npc_sex_id",
                        column: x => x.npc_sex_id,
                        principalSchema: "content",
                        principalTable: "npc_sexes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npcs_npc_types_npc_type_id",
                        column: x => x.npc_type_id,
                        principalSchema: "content",
                        principalTable: "npc_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_classes",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    player_sex_id = table.Column<int>(type: "integer", nullable: false),
                    player_race_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_mage = table.Column<bool>(type: "boolean", nullable: false),
                    parent_class_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_classes", x => new { x.id, x.player_sex_id, x.player_race_id });
                    table.ForeignKey(
                        name: "FK_player_classes_player_classes_parent_class_id_player_sex_id~",
                        columns: x => new { x.parent_class_id, x.player_sex_id, x.player_race_id },
                        principalSchema: "content",
                        principalTable: "player_classes",
                        principalColumns: new[] { "id", "player_sex_id", "player_race_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_classes_player_races_player_race_id",
                        column: x => x.player_race_id,
                        principalSchema: "content",
                        principalTable: "player_races",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_classes_player_sexes_player_sex_id",
                        column: x => x.player_sex_id,
                        principalSchema: "content",
                        principalTable: "player_sexes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_faces",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    player_sex_id = table.Column<int>(type: "integer", nullable: false),
                    player_race_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_faces", x => new { x.id, x.player_sex_id, x.player_race_id });
                    table.ForeignKey(
                        name: "FK_player_faces_player_races_player_race_id",
                        column: x => x.player_race_id,
                        principalSchema: "content",
                        principalTable: "player_races",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_faces_player_sexes_player_sex_id",
                        column: x => x.player_sex_id,
                        principalSchema: "content",
                        principalTable: "player_sexes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_hair_colors",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    player_sex_id = table.Column<int>(type: "integer", nullable: false),
                    player_race_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_hair_colors", x => new { x.id, x.player_sex_id, x.player_race_id });
                    table.ForeignKey(
                        name: "FK_player_hair_colors_player_races_player_race_id",
                        column: x => x.player_race_id,
                        principalSchema: "content",
                        principalTable: "player_races",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_hair_colors_player_sexes_player_sex_id",
                        column: x => x.player_sex_id,
                        principalSchema: "content",
                        principalTable: "player_sexes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_hair_styles",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    player_sex_id = table.Column<int>(type: "integer", nullable: false),
                    player_race_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_hair_styles", x => new { x.id, x.player_sex_id, x.player_race_id });
                    table.ForeignKey(
                        name: "FK_player_hair_styles_player_races_player_race_id",
                        column: x => x.player_race_id,
                        principalSchema: "content",
                        principalTable: "player_races",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_hair_styles_player_sexes_player_sex_id",
                        column: x => x.player_sex_id,
                        principalSchema: "content",
                        principalTable: "player_sexes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    levels = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    skill_operate_type_id = table.Column<int>(type: "integer", nullable: true),
                    skill_target_type_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.id);
                    table.CheckConstraint("ck_skills_levels", "levels BETWEEN 1 AND 255");
                    table.ForeignKey(
                        name: "FK_skills_skill_operate_types_skill_operate_type_id",
                        column: x => x.skill_operate_type_id,
                        principalSchema: "content",
                        principalTable: "skill_operate_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_skills_skill_target_types_skill_target_type_id",
                        column: x => x.skill_target_type_id,
                        principalSchema: "content",
                        principalTable: "skill_target_types",
                        principalColumn: "id",
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
                        name: "FK_asset_import_diagnostics_asset_import_runs_run_id",
                        column: x => x.run_id,
                        principalSchema: "content",
                        principalTable: "asset_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_asset_import_diagnostics_asset_import_work_items_work_item_~",
                        column: x => x.work_item_id,
                        principalSchema: "content",
                        principalTable: "asset_import_work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_icons",
                schema: "content",
                columns: table => new
                {
                    skill_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_icons", x => new { x.skill_id, x.level });
                    table.CheckConstraint("ck_skill_icons_level", "level BETWEEN 1 AND 255");
                    table.ForeignKey(
                        name: "FK_skill_icons_skills_skill_id",
                        column: x => x.skill_id,
                        principalSchema: "content",
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalog_groups_catalog_name",
                schema: "content",
                table: "asset_catalog_groups",
                columns: new[] { "catalog_id", "name" },
                unique: true);

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
                name: "ix_asset_catalog_sources_catalog_source",
                schema: "content",
                table: "asset_catalog_sources",
                columns: new[] { "catalog_id", "normalized_source_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalogs_active_kind",
                schema: "content",
                table: "asset_catalogs",
                column: "kind",
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

            migrationBuilder.Sql("""
                CREATE INDEX ix_asset_import_diagnostics_search
                ON content.asset_import_diagnostics
                USING GIN (to_tsvector('simple',
                    coalesce(source_key, '') || ' ' || coalesce(object_name, '') || ' ' || message));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_asset_import_diagnostics_work_item_id",
                schema: "content",
                table: "asset_import_diagnostics",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_runs_active_full_scan_kind",
                schema: "content",
                table: "asset_import_runs",
                column: "kind",
                unique: true,
                filter: "trigger_type = 'full_scan' AND status IN ('queued', 'discovering', 'running')");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_runs_active_single_source",
                schema: "content",
                table: "asset_import_runs",
                columns: new[] { "kind", "normalized_requested_source_key" },
                unique: true,
                filter: "trigger_type = 'single_file' AND status IN ('queued', 'discovering', 'running')");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_runs_kind_requested",
                schema: "content",
                table: "asset_import_runs",
                columns: new[] { "kind", "requested_at" });

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
                name: "ix_npc_races_name",
                schema: "content",
                table: "npc_races",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_npc_sexes_name",
                schema: "content",
                table: "npc_sexes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_npc_types_name",
                schema: "content",
                table: "npc_types",
                column: "name",
                unique: true);

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
                name: "ix_player_classes_name_sex_race",
                schema: "content",
                table: "player_classes",
                columns: new[] { "name", "player_sex_id", "player_race_id" },
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
                name: "IX_player_faces_player_race_id",
                schema: "content",
                table: "player_faces",
                column: "player_race_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_faces_player_sex_id",
                schema: "content",
                table: "player_faces",
                column: "player_sex_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_hair_colors_player_race_id",
                schema: "content",
                table: "player_hair_colors",
                column: "player_race_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_hair_colors_player_sex_id",
                schema: "content",
                table: "player_hair_colors",
                column: "player_sex_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_hair_styles_player_race_id",
                schema: "content",
                table: "player_hair_styles",
                column: "player_race_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_hair_styles_player_sex_id",
                schema: "content",
                table: "player_hair_styles",
                column: "player_sex_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_races_name",
                schema: "content",
                table: "player_races",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_sexes_name",
                schema: "content",
                table: "player_sexes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skill_operate_types_name",
                schema: "content",
                table: "skill_operate_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skill_target_types_name",
                schema: "content",
                table: "skill_target_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skills_skill_operate_type_id",
                schema: "content",
                table: "skills",
                column: "skill_operate_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_skills_skill_target_type_id",
                schema: "content",
                table: "skills",
                column: "skill_target_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_catalog_groups",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_catalog_items",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_import_diagnostics",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npcs",
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
                name: "asset_import_work_items",
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
                name: "asset_import_runs",
                schema: "content");

            migrationBuilder.DropTable(
                name: "skill_operate_types",
                schema: "content");

            migrationBuilder.DropTable(
                name: "skill_target_types",
                schema: "content");
        }
    }
}

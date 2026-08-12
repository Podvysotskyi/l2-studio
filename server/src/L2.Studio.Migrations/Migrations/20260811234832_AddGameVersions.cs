using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGameVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_npcs_npc_races_npc_race_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropForeignKey(
                name: "FK_npcs_npc_sexes_npc_sex_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropForeignKey(
                name: "FK_npcs_npc_types_npc_type_id",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropForeignKey(
                name: "FK_player_classes_player_classes_parent_class_id_player_sex_id~",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropForeignKey(
                name: "FK_player_classes_player_races_player_race_id",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropForeignKey(
                name: "FK_player_classes_player_sexes_player_sex_id",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropForeignKey(
                name: "FK_player_faces_player_races_player_race_id",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropForeignKey(
                name: "FK_player_faces_player_sexes_player_sex_id",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_colors_player_races_player_race_id",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_colors_player_sexes_player_sex_id",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_styles_player_races_player_race_id",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_styles_player_sexes_player_sex_id",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropForeignKey(
                name: "FK_skill_icons_skills_skill_id",
                schema: "content",
                table: "skill_icons");

            migrationBuilder.DropForeignKey(
                name: "FK_skills_skill_operate_types_skill_operate_type_id",
                schema: "content",
                table: "skills");

            migrationBuilder.DropForeignKey(
                name: "FK_skills_skill_target_types_skill_target_type_id",
                schema: "content",
                table: "skills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_skills",
                schema: "content",
                table: "skills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_skill_target_types",
                schema: "content",
                table: "skill_target_types");

            migrationBuilder.DropIndex(
                name: "ix_skill_target_types_name",
                schema: "content",
                table: "skill_target_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_skill_operate_types",
                schema: "content",
                table: "skill_operate_types");

            migrationBuilder.DropIndex(
                name: "ix_skill_operate_types_name",
                schema: "content",
                table: "skill_operate_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_skill_icons",
                schema: "content",
                table: "skill_icons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_sexes",
                schema: "content",
                table: "player_sexes");

            migrationBuilder.DropIndex(
                name: "ix_player_sexes_name",
                schema: "content",
                table: "player_sexes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_races",
                schema: "content",
                table: "player_races");

            migrationBuilder.DropIndex(
                name: "ix_player_races_name",
                schema: "content",
                table: "player_races");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_hair_styles",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropIndex(
                name: "IX_player_hair_styles_player_race_id",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropIndex(
                name: "IX_player_hair_styles_player_sex_id",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_hair_colors",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropIndex(
                name: "IX_player_hair_colors_player_race_id",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropIndex(
                name: "IX_player_hair_colors_player_sex_id",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_faces",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropIndex(
                name: "IX_player_faces_player_race_id",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropIndex(
                name: "IX_player_faces_player_sex_id",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_classes",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropIndex(
                name: "ix_player_classes_name_sex_race",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_npcs",
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

            migrationBuilder.DropIndex(
                name: "ix_asset_import_runs_active_full_scan_kind",
                schema: "content",
                table: "asset_import_runs");

            migrationBuilder.DropIndex(
                name: "ix_asset_import_runs_active_single_source",
                schema: "content",
                table: "asset_import_runs");

            migrationBuilder.DropIndex(
                name: "ix_asset_import_runs_kind_requested",
                schema: "content",
                table: "asset_import_runs");

            migrationBuilder.DropIndex(
                name: "ix_asset_catalogs_active_kind",
                schema: "content",
                table: "asset_catalogs");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "skills",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "skill_target_types",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "skill_operate_types",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "skill_icons",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "player_sexes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "player_races",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "player_hair_styles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "player_hair_colors",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "player_faces",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "player_classes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "npcs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "npc_types",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "npc_sexes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "npc_races",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "asset_import_work_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "asset_import_runs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddColumn<string>(
                name: "game_version",
                schema: "content",
                table: "asset_catalogs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "interlude");

            migrationBuilder.AddPrimaryKey(
                name: "PK_skills",
                schema: "content",
                table: "skills",
                columns: new[] { "game_version", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_skill_target_types",
                schema: "content",
                table: "skill_target_types",
                columns: new[] { "game_version", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_skill_operate_types",
                schema: "content",
                table: "skill_operate_types",
                columns: new[] { "game_version", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_skill_icons",
                schema: "content",
                table: "skill_icons",
                columns: new[] { "game_version", "skill_id", "level" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_sexes",
                schema: "content",
                table: "player_sexes",
                columns: new[] { "game_version", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_races",
                schema: "content",
                table: "player_races",
                columns: new[] { "game_version", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_hair_styles",
                schema: "content",
                table: "player_hair_styles",
                columns: new[] { "game_version", "id", "player_sex_id", "player_race_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_hair_colors",
                schema: "content",
                table: "player_hair_colors",
                columns: new[] { "game_version", "id", "player_sex_id", "player_race_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_faces",
                schema: "content",
                table: "player_faces",
                columns: new[] { "game_version", "id", "player_sex_id", "player_race_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_classes",
                schema: "content",
                table: "player_classes",
                columns: new[] { "game_version", "id", "player_sex_id", "player_race_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_npcs",
                schema: "content",
                table: "npcs",
                columns: new[] { "game_version", "id" });

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

            migrationBuilder.InsertData(
                schema: "content",
                table: "game_versions",
                columns: new[] { "key", "display_name", "sort_order", "source_folder" },
                values: new object[,]
                {
                    { "c1", "Chronicle 1", 10, "C1" },
                    { "c4", "Chronicle 4", 20, "C4" },
                    { "interlude", "Interlude", 30, "Interlude" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_skills_game_version_skill_operate_type_id",
                schema: "content",
                table: "skills",
                columns: new[] { "game_version", "skill_operate_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_skills_game_version_skill_target_type_id",
                schema: "content",
                table: "skills",
                columns: new[] { "game_version", "skill_target_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_target_types_name",
                schema: "content",
                table: "skill_target_types",
                columns: new[] { "game_version", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skill_operate_types_name",
                schema: "content",
                table: "skill_operate_types",
                columns: new[] { "game_version", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_sexes_name",
                schema: "content",
                table: "player_sexes",
                columns: new[] { "game_version", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_races_name",
                schema: "content",
                table: "player_races",
                columns: new[] { "game_version", "name" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_asset_import_work_items_game_version",
                schema: "content",
                table: "asset_import_work_items",
                column: "game_version");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_runs_active_full_scan_kind",
                schema: "content",
                table: "asset_import_runs",
                columns: new[] { "game_version", "kind" },
                unique: true,
                filter: "trigger_type = 'full_scan' AND status IN ('queued', 'discovering', 'running')");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_runs_active_single_source",
                schema: "content",
                table: "asset_import_runs",
                columns: new[] { "game_version", "kind", "normalized_requested_source_key" },
                unique: true,
                filter: "trigger_type = 'single_file' AND status IN ('queued', 'discovering', 'running')");

            migrationBuilder.CreateIndex(
                name: "ix_asset_import_runs_kind_requested",
                schema: "content",
                table: "asset_import_runs",
                columns: new[] { "game_version", "kind", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_catalogs_active_kind",
                schema: "content",
                table: "asset_catalogs",
                columns: new[] { "game_version", "kind" },
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_game_versions_display_name",
                schema: "content",
                table: "game_versions",
                column: "display_name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_asset_catalogs_game_versions_game_version",
                schema: "content",
                table: "asset_catalogs",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_asset_import_runs_game_versions_game_version",
                schema: "content",
                table: "asset_import_runs",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_asset_import_work_items_game_versions_game_version",
                schema: "content",
                table: "asset_import_work_items",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npc_races_game_versions_game_version",
                schema: "content",
                table: "npc_races",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npc_sexes_game_versions_game_version",
                schema: "content",
                table: "npc_sexes",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npc_types_game_versions_game_version",
                schema: "content",
                table: "npc_types",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_game_versions_game_version",
                schema: "content",
                table: "npcs",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

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

            migrationBuilder.AddForeignKey(
                name: "FK_player_classes_game_versions_game_version",
                schema: "content",
                table: "player_classes",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_classes_player_classes_game_version_parent_class_id_~",
                schema: "content",
                table: "player_classes",
                columns: new[] { "game_version", "parent_class_id", "player_sex_id", "player_race_id" },
                principalSchema: "content",
                principalTable: "player_classes",
                principalColumns: new[] { "game_version", "id", "player_sex_id", "player_race_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_classes_player_races_game_version_player_race_id",
                schema: "content",
                table: "player_classes",
                columns: new[] { "game_version", "player_race_id" },
                principalSchema: "content",
                principalTable: "player_races",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_classes_player_sexes_game_version_player_sex_id",
                schema: "content",
                table: "player_classes",
                columns: new[] { "game_version", "player_sex_id" },
                principalSchema: "content",
                principalTable: "player_sexes",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_faces_game_versions_game_version",
                schema: "content",
                table: "player_faces",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_faces_player_races_game_version_player_race_id",
                schema: "content",
                table: "player_faces",
                columns: new[] { "game_version", "player_race_id" },
                principalSchema: "content",
                principalTable: "player_races",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_faces_player_sexes_game_version_player_sex_id",
                schema: "content",
                table: "player_faces",
                columns: new[] { "game_version", "player_sex_id" },
                principalSchema: "content",
                principalTable: "player_sexes",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_colors_game_versions_game_version",
                schema: "content",
                table: "player_hair_colors",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_colors_player_races_game_version_player_race_id",
                schema: "content",
                table: "player_hair_colors",
                columns: new[] { "game_version", "player_race_id" },
                principalSchema: "content",
                principalTable: "player_races",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_colors_player_sexes_game_version_player_sex_id",
                schema: "content",
                table: "player_hair_colors",
                columns: new[] { "game_version", "player_sex_id" },
                principalSchema: "content",
                principalTable: "player_sexes",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_styles_game_versions_game_version",
                schema: "content",
                table: "player_hair_styles",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_styles_player_races_game_version_player_race_id",
                schema: "content",
                table: "player_hair_styles",
                columns: new[] { "game_version", "player_race_id" },
                principalSchema: "content",
                principalTable: "player_races",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_styles_player_sexes_game_version_player_sex_id",
                schema: "content",
                table: "player_hair_styles",
                columns: new[] { "game_version", "player_sex_id" },
                principalSchema: "content",
                principalTable: "player_sexes",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_races_game_versions_game_version",
                schema: "content",
                table: "player_races",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_sexes_game_versions_game_version",
                schema: "content",
                table: "player_sexes",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_skill_icons_game_versions_game_version",
                schema: "content",
                table: "skill_icons",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_skill_icons_skills_game_version_skill_id",
                schema: "content",
                table: "skill_icons",
                columns: new[] { "game_version", "skill_id" },
                principalSchema: "content",
                principalTable: "skills",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_skill_operate_types_game_versions_game_version",
                schema: "content",
                table: "skill_operate_types",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_skill_target_types_game_versions_game_version",
                schema: "content",
                table: "skill_target_types",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_skills_game_versions_game_version",
                schema: "content",
                table: "skills",
                column: "game_version",
                principalSchema: "content",
                principalTable: "game_versions",
                principalColumn: "key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_skills_skill_operate_types_game_version_skill_operate_type_~",
                schema: "content",
                table: "skills",
                columns: new[] { "game_version", "skill_operate_type_id" },
                principalSchema: "content",
                principalTable: "skill_operate_types",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_skills_skill_target_types_game_version_skill_target_type_id",
                schema: "content",
                table: "skills",
                columns: new[] { "game_version", "skill_target_type_id" },
                principalSchema: "content",
                principalTable: "skill_target_types",
                principalColumns: new[] { "game_version", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_asset_catalogs_game_versions_game_version",
                schema: "content",
                table: "asset_catalogs");

            migrationBuilder.DropForeignKey(
                name: "FK_asset_import_runs_game_versions_game_version",
                schema: "content",
                table: "asset_import_runs");

            migrationBuilder.DropForeignKey(
                name: "FK_asset_import_work_items_game_versions_game_version",
                schema: "content",
                table: "asset_import_work_items");

            migrationBuilder.DropForeignKey(
                name: "FK_npc_races_game_versions_game_version",
                schema: "content",
                table: "npc_races");

            migrationBuilder.DropForeignKey(
                name: "FK_npc_sexes_game_versions_game_version",
                schema: "content",
                table: "npc_sexes");

            migrationBuilder.DropForeignKey(
                name: "FK_npc_types_game_versions_game_version",
                schema: "content",
                table: "npc_types");

            migrationBuilder.DropForeignKey(
                name: "FK_npcs_game_versions_game_version",
                schema: "content",
                table: "npcs");

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

            migrationBuilder.DropForeignKey(
                name: "FK_player_classes_game_versions_game_version",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropForeignKey(
                name: "FK_player_classes_player_classes_game_version_parent_class_id_~",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropForeignKey(
                name: "FK_player_classes_player_races_game_version_player_race_id",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropForeignKey(
                name: "FK_player_classes_player_sexes_game_version_player_sex_id",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropForeignKey(
                name: "FK_player_faces_game_versions_game_version",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropForeignKey(
                name: "FK_player_faces_player_races_game_version_player_race_id",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropForeignKey(
                name: "FK_player_faces_player_sexes_game_version_player_sex_id",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_colors_game_versions_game_version",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_colors_player_races_game_version_player_race_id",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_colors_player_sexes_game_version_player_sex_id",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_styles_game_versions_game_version",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_styles_player_races_game_version_player_race_id",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropForeignKey(
                name: "FK_player_hair_styles_player_sexes_game_version_player_sex_id",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropForeignKey(
                name: "FK_player_races_game_versions_game_version",
                schema: "content",
                table: "player_races");

            migrationBuilder.DropForeignKey(
                name: "FK_player_sexes_game_versions_game_version",
                schema: "content",
                table: "player_sexes");

            migrationBuilder.DropForeignKey(
                name: "FK_skill_icons_game_versions_game_version",
                schema: "content",
                table: "skill_icons");

            migrationBuilder.DropForeignKey(
                name: "FK_skill_icons_skills_game_version_skill_id",
                schema: "content",
                table: "skill_icons");

            migrationBuilder.DropForeignKey(
                name: "FK_skill_operate_types_game_versions_game_version",
                schema: "content",
                table: "skill_operate_types");

            migrationBuilder.DropForeignKey(
                name: "FK_skill_target_types_game_versions_game_version",
                schema: "content",
                table: "skill_target_types");

            migrationBuilder.DropForeignKey(
                name: "FK_skills_game_versions_game_version",
                schema: "content",
                table: "skills");

            migrationBuilder.DropForeignKey(
                name: "FK_skills_skill_operate_types_game_version_skill_operate_type_~",
                schema: "content",
                table: "skills");

            migrationBuilder.DropForeignKey(
                name: "FK_skills_skill_target_types_game_version_skill_target_type_id",
                schema: "content",
                table: "skills");

            migrationBuilder.DropTable(
                name: "game_versions",
                schema: "content");

            migrationBuilder.DropPrimaryKey(
                name: "PK_skills",
                schema: "content",
                table: "skills");

            migrationBuilder.DropIndex(
                name: "IX_skills_game_version_skill_operate_type_id",
                schema: "content",
                table: "skills");

            migrationBuilder.DropIndex(
                name: "IX_skills_game_version_skill_target_type_id",
                schema: "content",
                table: "skills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_skill_target_types",
                schema: "content",
                table: "skill_target_types");

            migrationBuilder.DropIndex(
                name: "ix_skill_target_types_name",
                schema: "content",
                table: "skill_target_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_skill_operate_types",
                schema: "content",
                table: "skill_operate_types");

            migrationBuilder.DropIndex(
                name: "ix_skill_operate_types_name",
                schema: "content",
                table: "skill_operate_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_skill_icons",
                schema: "content",
                table: "skill_icons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_sexes",
                schema: "content",
                table: "player_sexes");

            migrationBuilder.DropIndex(
                name: "ix_player_sexes_name",
                schema: "content",
                table: "player_sexes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_races",
                schema: "content",
                table: "player_races");

            migrationBuilder.DropIndex(
                name: "ix_player_races_name",
                schema: "content",
                table: "player_races");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_hair_styles",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropIndex(
                name: "IX_player_hair_styles_game_version_player_race_id",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropIndex(
                name: "IX_player_hair_styles_game_version_player_sex_id",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_hair_colors",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropIndex(
                name: "IX_player_hair_colors_game_version_player_race_id",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropIndex(
                name: "IX_player_hair_colors_game_version_player_sex_id",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_faces",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropIndex(
                name: "IX_player_faces_game_version_player_race_id",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropIndex(
                name: "IX_player_faces_game_version_player_sex_id",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_classes",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropIndex(
                name: "IX_player_classes_game_version_parent_class_id_player_sex_id_p~",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropIndex(
                name: "IX_player_classes_game_version_player_race_id",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropIndex(
                name: "IX_player_classes_game_version_player_sex_id",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropIndex(
                name: "ix_player_classes_name_sex_race",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_npcs",
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

            migrationBuilder.DropIndex(
                name: "IX_asset_import_work_items_game_version",
                schema: "content",
                table: "asset_import_work_items");

            migrationBuilder.DropIndex(
                name: "ix_asset_import_runs_active_full_scan_kind",
                schema: "content",
                table: "asset_import_runs");

            migrationBuilder.DropIndex(
                name: "ix_asset_import_runs_active_single_source",
                schema: "content",
                table: "asset_import_runs");

            migrationBuilder.DropIndex(
                name: "ix_asset_import_runs_kind_requested",
                schema: "content",
                table: "asset_import_runs");

            migrationBuilder.DropIndex(
                name: "ix_asset_catalogs_active_kind",
                schema: "content",
                table: "asset_catalogs");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "skill_target_types");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "skill_operate_types");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "skill_icons");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "player_sexes");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "player_races");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "player_hair_styles");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "player_hair_colors");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "player_faces");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "player_classes");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "npcs");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "npc_types");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "npc_sexes");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "npc_races");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "asset_import_work_items");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "asset_import_runs");

            migrationBuilder.DropColumn(
                name: "game_version",
                schema: "content",
                table: "asset_catalogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_skills",
                schema: "content",
                table: "skills",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_skill_target_types",
                schema: "content",
                table: "skill_target_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_skill_operate_types",
                schema: "content",
                table: "skill_operate_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_skill_icons",
                schema: "content",
                table: "skill_icons",
                columns: new[] { "skill_id", "level" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_sexes",
                schema: "content",
                table: "player_sexes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_races",
                schema: "content",
                table: "player_races",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_hair_styles",
                schema: "content",
                table: "player_hair_styles",
                columns: new[] { "id", "player_sex_id", "player_race_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_hair_colors",
                schema: "content",
                table: "player_hair_colors",
                columns: new[] { "id", "player_sex_id", "player_race_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_faces",
                schema: "content",
                table: "player_faces",
                columns: new[] { "id", "player_sex_id", "player_race_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_classes",
                schema: "content",
                table: "player_classes",
                columns: new[] { "id", "player_sex_id", "player_race_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_npcs",
                schema: "content",
                table: "npcs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_types",
                schema: "content",
                table: "npc_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_sexes",
                schema: "content",
                table: "npc_sexes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_races",
                schema: "content",
                table: "npc_races",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_skill_target_types_name",
                schema: "content",
                table: "skill_target_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skill_operate_types_name",
                schema: "content",
                table: "skill_operate_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_sexes_name",
                schema: "content",
                table: "player_sexes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_races_name",
                schema: "content",
                table: "player_races",
                column: "name",
                unique: true);

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
                name: "ix_player_classes_name_sex_race",
                schema: "content",
                table: "player_classes",
                columns: new[] { "name", "player_sex_id", "player_race_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_npc_types_name",
                schema: "content",
                table: "npc_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_npc_sexes_name",
                schema: "content",
                table: "npc_sexes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_npc_races_name",
                schema: "content",
                table: "npc_races",
                column: "name",
                unique: true);

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
                name: "ix_asset_catalogs_active_kind",
                schema: "content",
                table: "asset_catalogs",
                column: "kind",
                unique: true,
                filter: "is_active");

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_npc_races_npc_race_id",
                schema: "content",
                table: "npcs",
                column: "npc_race_id",
                principalSchema: "content",
                principalTable: "npc_races",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_npc_sexes_npc_sex_id",
                schema: "content",
                table: "npcs",
                column: "npc_sex_id",
                principalSchema: "content",
                principalTable: "npc_sexes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_npcs_npc_types_npc_type_id",
                schema: "content",
                table: "npcs",
                column: "npc_type_id",
                principalSchema: "content",
                principalTable: "npc_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_classes_player_classes_parent_class_id_player_sex_id~",
                schema: "content",
                table: "player_classes",
                columns: new[] { "parent_class_id", "player_sex_id", "player_race_id" },
                principalSchema: "content",
                principalTable: "player_classes",
                principalColumns: new[] { "id", "player_sex_id", "player_race_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_classes_player_races_player_race_id",
                schema: "content",
                table: "player_classes",
                column: "player_race_id",
                principalSchema: "content",
                principalTable: "player_races",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_classes_player_sexes_player_sex_id",
                schema: "content",
                table: "player_classes",
                column: "player_sex_id",
                principalSchema: "content",
                principalTable: "player_sexes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_faces_player_races_player_race_id",
                schema: "content",
                table: "player_faces",
                column: "player_race_id",
                principalSchema: "content",
                principalTable: "player_races",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_faces_player_sexes_player_sex_id",
                schema: "content",
                table: "player_faces",
                column: "player_sex_id",
                principalSchema: "content",
                principalTable: "player_sexes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_colors_player_races_player_race_id",
                schema: "content",
                table: "player_hair_colors",
                column: "player_race_id",
                principalSchema: "content",
                principalTable: "player_races",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_colors_player_sexes_player_sex_id",
                schema: "content",
                table: "player_hair_colors",
                column: "player_sex_id",
                principalSchema: "content",
                principalTable: "player_sexes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_styles_player_races_player_race_id",
                schema: "content",
                table: "player_hair_styles",
                column: "player_race_id",
                principalSchema: "content",
                principalTable: "player_races",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_hair_styles_player_sexes_player_sex_id",
                schema: "content",
                table: "player_hair_styles",
                column: "player_sex_id",
                principalSchema: "content",
                principalTable: "player_sexes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_skill_icons_skills_skill_id",
                schema: "content",
                table: "skill_icons",
                column: "skill_id",
                principalSchema: "content",
                principalTable: "skills",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_skills_skill_operate_types_skill_operate_type_id",
                schema: "content",
                table: "skills",
                column: "skill_operate_type_id",
                principalSchema: "content",
                principalTable: "skill_operate_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_skills_skill_target_types_skill_target_type_id",
                schema: "content",
                table: "skills",
                column: "skill_target_type_id",
                principalSchema: "content",
                principalTable: "skill_target_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ManageAssetReleases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "asset_releases",
                schema: "content");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RegisterGeneratedArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "artifact_id",
                schema: "content",
                table: "asset_catalog_sources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

            migrationBuilder.CreateIndex(
                name: "IX_asset_catalog_sources_artifact_id",
                schema: "content",
                table: "asset_catalog_sources",
                column: "artifact_id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_asset_catalog_sources_asset_artifacts_artifact_id",
                schema: "content",
                table: "asset_catalog_sources",
                column: "artifact_id",
                principalSchema: "content",
                principalTable: "asset_artifacts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_asset_catalog_sources_asset_artifacts_artifact_id",
                schema: "content",
                table: "asset_catalog_sources");

            migrationBuilder.DropTable(
                name: "asset_artifact_dependencies",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_artifact_files",
                schema: "content");

            migrationBuilder.DropTable(
                name: "asset_artifacts",
                schema: "content");

            migrationBuilder.DropIndex(
                name: "IX_asset_catalog_sources_artifact_id",
                schema: "content",
                table: "asset_catalog_sources");

            migrationBuilder.DropColumn(
                name: "artifact_id",
                schema: "content",
                table: "asset_catalog_sources");
        }
    }
}

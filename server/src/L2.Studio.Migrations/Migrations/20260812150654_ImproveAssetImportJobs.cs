using L2.Studio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace L2.Studio.Migrations.Migrations;

[DbContext(typeof(GameContentDbContext))]
[Migration("20260812150654_ImproveAssetImportJobs")]
public partial class ImproveAssetImportJobs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "artifact_fingerprint", schema: "content", table: "asset_import_work_items", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "last_heartbeat_at", schema: "content", table: "asset_import_work_items", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "force", schema: "content", table: "asset_import_runs", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "last_heartbeat_at", schema: "content", table: "asset_import_runs", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<int>(name: "reused_file_count", schema: "content", table: "asset_import_runs", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "artifact_fingerprint", schema: "content", table: "asset_catalog_sources", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "is_stale", schema: "content", table: "asset_catalog_sources", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "stale_at", schema: "content", table: "asset_catalog_sources", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "stale_reasons_json", schema: "content", table: "asset_catalog_sources", type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb");

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
                table.PrimaryKey("PK_asset_catalog_source_dependencies", value => value.id);
                table.ForeignKey(
                    "FK_asset_catalog_source_dependencies_asset_catalog_sources_source_id",
                    value => value.source_id,
                    "content",
                    "asset_catalog_sources",
                    "id",
                    onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "ix_asset_catalog_source_dependencies_key", schema: "content", table: "asset_catalog_source_dependencies", columns: new[] { "kind", "dependency_key" });
        migrationBuilder.CreateIndex(name: "ix_asset_catalog_source_dependencies_source", schema: "content", table: "asset_catalog_source_dependencies", columns: new[] { "kind", "resolved_source_key" });
        migrationBuilder.CreateIndex(name: "IX_asset_catalog_source_dependencies_source_id", schema: "content", table: "asset_catalog_source_dependencies", column: "source_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("asset_catalog_source_dependencies", "content");
        migrationBuilder.DropColumn("artifact_fingerprint", "content", "asset_import_work_items");
        migrationBuilder.DropColumn("last_heartbeat_at", "content", "asset_import_work_items");
        migrationBuilder.DropColumn("force", "content", "asset_import_runs");
        migrationBuilder.DropColumn("last_heartbeat_at", "content", "asset_import_runs");
        migrationBuilder.DropColumn("reused_file_count", "content", "asset_import_runs");
        migrationBuilder.DropColumn("artifact_fingerprint", "content", "asset_catalog_sources");
        migrationBuilder.DropColumn("is_stale", "content", "asset_catalog_sources");
        migrationBuilder.DropColumn("stale_at", "content", "asset_catalog_sources");
        migrationBuilder.DropColumn("stale_reasons_json", "content", "asset_catalog_sources");
    }
}

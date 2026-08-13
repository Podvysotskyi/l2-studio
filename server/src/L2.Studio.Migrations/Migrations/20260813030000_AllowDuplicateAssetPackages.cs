using L2.Studio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations;

[DbContext(typeof(GameContentDbContext))]
[Migration("20260813030000_AllowDuplicateAssetPackages")]
public partial class AllowDuplicateAssetPackages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_asset_catalog_groups_catalog_name",
            schema: "content",
            table: "asset_catalog_groups");
        migrationBuilder.CreateIndex(
            name: "ix_asset_catalog_groups_catalog_name",
            schema: "content",
            table: "asset_catalog_groups",
            columns: ["catalog_id", "name"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_asset_catalog_groups_catalog_name",
            schema: "content",
            table: "asset_catalog_groups");
        migrationBuilder.CreateIndex(
            name: "ix_asset_catalog_groups_catalog_name",
            schema: "content",
            table: "asset_catalog_groups",
            columns: ["catalog_id", "name"],
            unique: true);
    }
}

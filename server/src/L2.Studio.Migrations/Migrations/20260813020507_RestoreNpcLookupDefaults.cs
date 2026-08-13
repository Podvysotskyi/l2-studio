using L2.Studio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations;

[DbContext(typeof(GameContentDbContext))]
[Migration("20260813020507_RestoreNpcLookupDefaults")]
public partial class RestoreNpcLookupDefaults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "mode",
            schema: "content",
            table: "npc_lookup_import_runs",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "add_missing");

        migrationBuilder.AddColumn<int>(
            name: "restored_count",
            schema: "content",
            table: "npc_lookup_import_runs",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "mode", schema: "content", table: "npc_lookup_import_runs");
        migrationBuilder.DropColumn(name: "restored_count", schema: "content", table: "npc_lookup_import_runs");
    }
}

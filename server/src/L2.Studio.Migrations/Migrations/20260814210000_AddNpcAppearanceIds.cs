using L2.Studio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations;

[DbContext(typeof(GameContentDbContext))]
[Migration("20260814210000_AddNpcAppearanceIds")]
public partial class AddNpcAppearanceIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "appearance_id",
            schema: "content",
            table: "npcs",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "appearance_id", schema: "content", table: "npcs");
    }
}

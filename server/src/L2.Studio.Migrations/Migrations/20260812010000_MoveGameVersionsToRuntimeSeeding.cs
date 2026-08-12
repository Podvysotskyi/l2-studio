using L2.Studio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations;

[DbContext(typeof(GameContentDbContext))]
[Migration("20260812010000_MoveGameVersionsToRuntimeSeeding")]
public partial class MoveGameVersionsToRuntimeSeeding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}

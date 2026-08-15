using L2.Studio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations;

[DbContext(typeof(GameContentDbContext))]
[Migration("20260814220000_AddNpcStatuses")]
public partial class AddNpcStatuses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "npc_statuses", schema: "content");
    }
}

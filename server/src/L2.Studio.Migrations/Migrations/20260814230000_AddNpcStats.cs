using L2.Studio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations;

[DbContext(typeof(GameContentDbContext))]
[Migration("20260814230000_AddNpcStats")]
public partial class AddNpcStats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "npc_stats",
            schema: "content",
            columns: table => new
            {
                game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                npc_id = table.Column<int>(type: "integer", nullable: false),
                str = table.Column<int>(type: "integer", nullable: true),
                @int = table.Column<int>(type: "integer", nullable: true),
                dex = table.Column<int>(type: "integer", nullable: true),
                wit = table.Column<int>(type: "integer", nullable: true),
                con = table.Column<int>(type: "integer", nullable: true),
                men = table.Column<int>(type: "integer", nullable: true),
                hit_time = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_npc_stats", x => new { x.game_version, x.npc_id });
                table.ForeignKey(name: "FK_npc_stats_game_versions_game_version", column: x => x.game_version, principalSchema: "content", principalTable: "game_versions", principalColumn: "key", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_npc_stats_npcs_game_version_npc_id", columns: x => new { x.game_version, x.npc_id }, principalSchema: "content", principalTable: "npcs", principalColumns: new[] { "game_version", "id" }, onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "npc_stats_vitals",
            schema: "content",
            columns: table => new
            {
                game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                npc_id = table.Column<int>(type: "integer", nullable: false),
                hp = table.Column<decimal>(type: "numeric", nullable: true),
                hp_regen = table.Column<decimal>(type: "numeric", nullable: true),
                mp = table.Column<decimal>(type: "numeric", nullable: true),
                mp_regen = table.Column<decimal>(type: "numeric", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_npc_stats_vitals", x => new { x.game_version, x.npc_id });
                table.ForeignKey(name: "FK_npc_stats_vitals_game_versions_game_version", column: x => x.game_version, principalSchema: "content", principalTable: "game_versions", principalColumn: "key", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_npc_stats_vitals_npcs_game_version_npc_id", columns: x => new { x.game_version, x.npc_id }, principalSchema: "content", principalTable: "npcs", principalColumns: new[] { "game_version", "id" }, onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "npc_stats_attack",
            schema: "content",
            columns: table => new
            {
                game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                npc_id = table.Column<int>(type: "integer", nullable: false),
                physical = table.Column<decimal>(type: "numeric", nullable: true),
                magical = table.Column<decimal>(type: "numeric", nullable: true),
                random = table.Column<int>(type: "integer", nullable: true),
                critical = table.Column<int>(type: "integer", nullable: true),
                accuracy = table.Column<decimal>(type: "numeric", nullable: true),
                attack_speed = table.Column<int>(type: "integer", nullable: true),
                reuse_delay = table.Column<int>(type: "integer", nullable: true),
                type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                range = table.Column<int>(type: "integer", nullable: true),
                distance = table.Column<int>(type: "integer", nullable: true),
                width = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_npc_stats_attack", x => new { x.game_version, x.npc_id });
                table.ForeignKey(name: "FK_npc_stats_attack_game_versions_game_version", column: x => x.game_version, principalSchema: "content", principalTable: "game_versions", principalColumn: "key", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_npc_stats_attack_npcs_game_version_npc_id", columns: x => new { x.game_version, x.npc_id }, principalSchema: "content", principalTable: "npcs", principalColumns: new[] { "game_version", "id" }, onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "npc_stats_defence",
            schema: "content",
            columns: table => new
            {
                game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                npc_id = table.Column<int>(type: "integer", nullable: false),
                physical = table.Column<decimal>(type: "numeric", nullable: true),
                magical = table.Column<decimal>(type: "numeric", nullable: true),
                evasion = table.Column<int>(type: "integer", nullable: true),
                shield = table.Column<int>(type: "integer", nullable: true),
                shield_rate = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_npc_stats_defence", x => new { x.game_version, x.npc_id });
                table.ForeignKey(name: "FK_npc_stats_defence_game_versions_game_version", column: x => x.game_version, principalSchema: "content", principalTable: "game_versions", principalColumn: "key", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_npc_stats_defence_npcs_game_version_npc_id", columns: x => new { x.game_version, x.npc_id }, principalSchema: "content", principalTable: "npcs", principalColumns: new[] { "game_version", "id" }, onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "npc_stats_speed",
            schema: "content",
            columns: table => new
            {
                game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                npc_id = table.Column<int>(type: "integer", nullable: false),
                walk_ground = table.Column<decimal>(type: "numeric", nullable: true),
                run_ground = table.Column<decimal>(type: "numeric", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_npc_stats_speed", x => new { x.game_version, x.npc_id });
                table.ForeignKey(name: "FK_npc_stats_speed_game_versions_game_version", column: x => x.game_version, principalSchema: "content", principalTable: "game_versions", principalColumn: "key", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_npc_stats_speed_npcs_game_version_npc_id", columns: x => new { x.game_version, x.npc_id }, principalSchema: "content", principalTable: "npcs", principalColumns: new[] { "game_version", "id" }, onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "npc_stats", schema: "content");
        migrationBuilder.DropTable(name: "npc_stats_vitals", schema: "content");
        migrationBuilder.DropTable(name: "npc_stats_attack", schema: "content");
        migrationBuilder.DropTable(name: "npc_stats_defence", schema: "content");
        migrationBuilder.DropTable(name: "npc_stats_speed", schema: "content");
    }
}

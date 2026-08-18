using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcSpawns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "npc_spawn_zones",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_spawn_zones", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_npc_spawn_zones_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "npc_spawns",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_spawns", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_npc_spawns_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "npc_spawn_zone_entities",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_spawn_zone_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    npc_id = table.Column<int>(type: "integer", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    respawn_delay_seconds = table.Column<int>(type: "integer", nullable: false),
                    respawn_random_seconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_spawn_zone_entities", x => new { x.game_version, x.npc_spawn_zone_name, x.sequence });
                    table.CheckConstraint("ck_npc_spawn_zone_entities_count", "count > 0");
                    table.ForeignKey(
                        name: "FK_npc_spawn_zone_entities_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_spawn_zone_entities_npc_spawn_zones_game_version_npc_sp~",
                        columns: x => new { x.game_version, x.npc_spawn_zone_name },
                        principalSchema: "content",
                        principalTable: "npc_spawn_zones",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_spawn_zone_territories",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_spawn_zone_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    min_z = table.Column<short>(type: "smallint", nullable: false),
                    max_z = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_spawn_zone_territories", x => new { x.game_version, x.npc_spawn_zone_name });
                    table.CheckConstraint("ck_npc_spawn_zone_territories_z_bounds", "min_z <= max_z");
                    table.ForeignKey(
                        name: "FK_npc_spawn_zone_territories_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_spawn_zone_territories_npc_spawn_zones_game_version_npc~",
                        columns: x => new { x.game_version, x.npc_spawn_zone_name },
                        principalSchema: "content",
                        principalTable: "npc_spawn_zones",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_spawn_entities",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_spawn_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    npc_id = table.Column<int>(type: "integer", nullable: false),
                    x = table.Column<int>(type: "integer", nullable: false),
                    y = table.Column<int>(type: "integer", nullable: false),
                    z = table.Column<int>(type: "integer", nullable: false),
                    heading = table.Column<int>(type: "integer", nullable: false),
                    respawn_delay_seconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_spawn_entities", x => new { x.game_version, x.npc_spawn_name, x.sequence });
                    table.ForeignKey(
                        name: "FK_npc_spawn_entities_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_spawn_entities_npc_spawns_game_version_npc_spawn_name",
                        columns: x => new { x.game_version, x.npc_spawn_name },
                        principalSchema: "content",
                        principalTable: "npc_spawns",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_spawn_zone_territory_nodes",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    npc_spawn_zone_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    x = table.Column<int>(type: "integer", nullable: false),
                    y = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_spawn_zone_territory_nodes", x => new { x.game_version, x.npc_spawn_zone_name, x.sequence });
                    table.ForeignKey(
                        name: "FK_npc_spawn_zone_territory_nodes_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_npc_spawn_zone_territory_nodes_npc_spawn_zone_territories_g~",
                        columns: x => new { x.game_version, x.npc_spawn_zone_name },
                        principalSchema: "content",
                        principalTable: "npc_spawn_zone_territories",
                        principalColumns: new[] { "game_version", "npc_spawn_zone_name" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_npc_spawn_entities_npc_id",
                schema: "content",
                table: "npc_spawn_entities",
                columns: new[] { "game_version", "npc_id" });

            migrationBuilder.CreateIndex(
                name: "ix_npc_spawn_zone_entities_npc_id",
                schema: "content",
                table: "npc_spawn_zone_entities",
                columns: new[] { "game_version", "npc_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "npc_spawn_entities",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_spawn_zone_entities",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_spawn_zone_territory_nodes",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_spawns",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_spawn_zone_territories",
                schema: "content");

            migrationBuilder.DropTable(
                name: "npc_spawn_zones",
                schema: "content");
        }
    }
}

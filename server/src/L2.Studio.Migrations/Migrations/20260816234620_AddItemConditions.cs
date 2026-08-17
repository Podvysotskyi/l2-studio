using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_conditions",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    message_id = table.Column<int>(type: "integer", nullable: false),
                    add_name = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_conditions", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_conditions_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_conditions_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_condition_players",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    is_pvp_flagged = table.Column<bool>(type: "boolean", nullable: true),
                    player_races = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    player_category_types = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_condition_players", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_condition_players_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_condition_players_item_conditions_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "item_conditions",
                        principalColumns: new[] { "game_version", "item_id" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_condition_players",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_conditions",
                schema: "content");
        }
    }
}

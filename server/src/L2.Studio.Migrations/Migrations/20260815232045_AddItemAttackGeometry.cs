using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAttackGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_attack_geometries",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    offset_x = table.Column<int>(type: "integer", nullable: false),
                    offset_y = table.Column<int>(type: "integer", nullable: false),
                    radius = table.Column<int>(type: "integer", nullable: false),
                    length = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_attack_geometries", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_attack_geometries_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM content.items
                        WHERE damage_range IS NOT NULL
                            AND btrim(damage_range) <> ''
                            AND btrim(damage_range) !~ '^-?[0-9]+;-?[0-9]+;-?[0-9]+;-?[0-9]+$'
                    ) THEN
                        RAISE EXCEPTION 'Cannot migrate content.items.damage_range because one or more values are not four semicolon-separated integers.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                INSERT INTO content.item_attack_geometries (game_version, item_id, offset_x, offset_y, radius, length)
                SELECT
                    game_version,
                    id,
                    split_part(btrim(damage_range), ';', 1)::integer,
                    split_part(btrim(damage_range), ';', 2)::integer,
                    split_part(btrim(damage_range), ';', 3)::integer,
                    split_part(btrim(damage_range), ';', 4)::integer
                FROM content.items
                WHERE damage_range IS NOT NULL AND btrim(damage_range) <> '';
                """);

            migrationBuilder.DropColumn(
                name: "damage_range",
                schema: "content",
                table: "items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "damage_range",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE content.items AS item
                SET damage_range = geometry.offset_x || ';' || geometry.offset_y || ';' || geometry.radius || ';' || geometry.length
                FROM content.item_attack_geometries AS geometry
                WHERE item.game_version = geometry.game_version AND item.id = geometry.item_id;
                """);

            migrationBuilder.DropTable(
                name: "item_attack_geometries",
                schema: "content");
        }
    }
}

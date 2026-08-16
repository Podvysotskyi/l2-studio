using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarchicalItemTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "parent_type_name",
                schema: "content",
                table: "item_types",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM content.items
                        WHERE ((weapon_type IS NOT NULL)::integer + (armor_type IS NOT NULL)::integer + (etcitem_type IS NOT NULL)::integer) > 1)
                    THEN
                        RAISE EXCEPTION 'An item cannot have more than one legacy subtype.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM (
                            SELECT game_version, COALESCE(weapon_type, armor_type, etcitem_type) AS subtype
                            FROM content.items
                            WHERE COALESCE(weapon_type, armor_type, etcitem_type) IS NOT NULL
                            GROUP BY game_version, item_type_name, COALESCE(weapon_type, armor_type, etcitem_type)
                        ) AS definitions
                        GROUP BY game_version, subtype
                        HAVING COUNT(*) > 1)
                    THEN
                        RAISE EXCEPTION 'A bare item subtype is assigned to multiple parent types.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                INSERT INTO content.item_types (game_version, name, display_name, parent_type_name)
                SELECT game_version, subtype, initcap(replace(subtype, '_', ' ')), parent_type_name
                FROM (
                    SELECT DISTINCT game_version, item_type_name AS parent_type_name,
                        COALESCE(weapon_type, armor_type, etcitem_type) AS subtype
                    FROM content.items
                    WHERE COALESCE(weapon_type, armor_type, etcitem_type) IS NOT NULL
                ) AS definitions
                ON CONFLICT (game_version, name) DO UPDATE
                SET parent_type_name = EXCLUDED.parent_type_name;

                UPDATE content.items
                SET item_type_name = COALESCE(weapon_type, armor_type, etcitem_type)
                WHERE COALESCE(weapon_type, armor_type, etcitem_type) IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "armor_type",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "etcitem_type",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "weapon_type",
                schema: "content",
                table: "items");

            migrationBuilder.CreateIndex(
                name: "ix_item_types_parent_type_name",
                schema: "content",
                table: "item_types",
                columns: new[] { "game_version", "parent_type_name" });

            migrationBuilder.AddForeignKey(
                name: "FK_item_types_item_types_game_version_parent_type_name",
                schema: "content",
                table: "item_types",
                columns: new[] { "game_version", "parent_type_name" },
                principalSchema: "content",
                principalTable: "item_types",
                principalColumns: new[] { "game_version", "name" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "armor_type",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "etcitem_type",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "weapon_type",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE content.items AS item
                SET weapon_type = CASE WHEN type.parent_type_name = 'Weapon' THEN item.item_type_name END,
                    armor_type = CASE WHEN type.parent_type_name = 'Armor' THEN item.item_type_name END,
                    etcitem_type = CASE WHEN type.parent_type_name = 'EtcItem' THEN item.item_type_name END,
                    item_type_name = COALESCE(type.parent_type_name, item.item_type_name)
                FROM content.item_types AS type
                WHERE type.game_version = item.game_version
                    AND type.name = item.item_type_name;

                DELETE FROM content.item_types
                WHERE parent_type_name IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_item_types_item_types_game_version_parent_type_name",
                schema: "content",
                table: "item_types");

            migrationBuilder.DropIndex(
                name: "ix_item_types_parent_type_name",
                schema: "content",
                table: "item_types");

            migrationBuilder.DropColumn(
                name: "parent_type_name",
                schema: "content",
                table: "item_types");
        }
    }
}

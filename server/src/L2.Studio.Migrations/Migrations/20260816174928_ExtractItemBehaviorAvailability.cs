using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ExtractItemBehaviorAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "enchant_enabled",
                schema: "content",
                table: "item_weapon");

            migrationBuilder.DropColumn(
                name: "for_npc",
                schema: "content",
                table: "item_weapon");

            migrationBuilder.DropColumn(
                name: "immediate_effect",
                schema: "content",
                table: "item_weapon");

            migrationBuilder.DropColumn(
                name: "is_depositable",
                schema: "content",
                table: "item_weapon");

            migrationBuilder.DropColumn(
                name: "is_destroyable",
                schema: "content",
                table: "item_weapon");

            migrationBuilder.DropColumn(
                name: "is_dropable",
                schema: "content",
                table: "item_weapon");

            migrationBuilder.DropColumn(
                name: "is_sellable",
                schema: "content",
                table: "item_weapon");

            migrationBuilder.DropColumn(
                name: "is_tradable",
                schema: "content",
                table: "item_weapon");

            migrationBuilder.DropColumn(
                name: "for_npc",
                schema: "content",
                table: "item_scroll");

            migrationBuilder.DropColumn(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_scroll");

            migrationBuilder.DropColumn(
                name: "is_stackable",
                schema: "content",
                table: "item_scroll");

            migrationBuilder.DropColumn(
                name: "immediate_effect",
                schema: "content",
                table: "item_recipe");

            migrationBuilder.DropColumn(
                name: "is_depositable",
                schema: "content",
                table: "item_recipe");

            migrationBuilder.DropColumn(
                name: "is_destroyable",
                schema: "content",
                table: "item_recipe");

            migrationBuilder.DropColumn(
                name: "is_dropable",
                schema: "content",
                table: "item_recipe");

            migrationBuilder.DropColumn(
                name: "is_sellable",
                schema: "content",
                table: "item_recipe");

            migrationBuilder.DropColumn(
                name: "is_stackable",
                schema: "content",
                table: "item_recipe");

            migrationBuilder.DropColumn(
                name: "is_tradable",
                schema: "content",
                table: "item_recipe");

            migrationBuilder.DropColumn(
                name: "for_npc",
                schema: "content",
                table: "item_potion");

            migrationBuilder.DropColumn(
                name: "immediate_effect",
                schema: "content",
                table: "item_potion");

            migrationBuilder.DropColumn(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_potion");

            migrationBuilder.DropColumn(
                name: "is_stackable",
                schema: "content",
                table: "item_potion");

            migrationBuilder.DropColumn(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_pet_collar");

            migrationBuilder.DropColumn(
                name: "immediate_effect",
                schema: "content",
                table: "item_material");

            migrationBuilder.DropColumn(
                name: "is_stackable",
                schema: "content",
                table: "item_material");

            migrationBuilder.DropColumn(
                name: "for_npc",
                schema: "content",
                table: "item_etc");

            migrationBuilder.DropColumn(
                name: "immediate_effect",
                schema: "content",
                table: "item_etc");

            migrationBuilder.DropColumn(
                name: "is_depositable",
                schema: "content",
                table: "item_etc");

            migrationBuilder.DropColumn(
                name: "is_destroyable",
                schema: "content",
                table: "item_etc");

            migrationBuilder.DropColumn(
                name: "is_dropable",
                schema: "content",
                table: "item_etc");

            migrationBuilder.DropColumn(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_etc");

            migrationBuilder.DropColumn(
                name: "is_sellable",
                schema: "content",
                table: "item_etc");

            migrationBuilder.DropColumn(
                name: "is_stackable",
                schema: "content",
                table: "item_etc");

            migrationBuilder.DropColumn(
                name: "is_tradable",
                schema: "content",
                table: "item_etc");

            migrationBuilder.DropColumn(
                name: "immediate_effect",
                schema: "content",
                table: "item_enchant");

            migrationBuilder.DropColumn(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_enchant");

            migrationBuilder.DropColumn(
                name: "is_stackable",
                schema: "content",
                table: "item_enchant");

            migrationBuilder.DropColumn(
                name: "immediate_effect",
                schema: "content",
                table: "item_arrow");

            migrationBuilder.DropColumn(
                name: "is_stackable",
                schema: "content",
                table: "item_arrow");

            migrationBuilder.DropColumn(
                name: "enchant_enabled",
                schema: "content",
                table: "item_armor");

            migrationBuilder.DropColumn(
                name: "for_npc",
                schema: "content",
                table: "item_armor");

            migrationBuilder.DropColumn(
                name: "immediate_effect",
                schema: "content",
                table: "item_armor");

            migrationBuilder.DropColumn(
                name: "is_depositable",
                schema: "content",
                table: "item_armor");

            migrationBuilder.DropColumn(
                name: "is_destroyable",
                schema: "content",
                table: "item_armor");

            migrationBuilder.DropColumn(
                name: "is_dropable",
                schema: "content",
                table: "item_armor");

            migrationBuilder.DropColumn(
                name: "is_sellable",
                schema: "content",
                table: "item_armor");

            migrationBuilder.DropColumn(
                name: "is_tradable",
                schema: "content",
                table: "item_armor");

            migrationBuilder.CreateTable(
                name: "item_behavior_availability",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    enchant_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    for_npc = table.Column<bool>(type: "boolean", nullable: true),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_depositable = table.Column<bool>(type: "boolean", nullable: true),
                    is_destroyable = table.Column<bool>(type: "boolean", nullable: true),
                    is_dropable = table.Column<bool>(type: "boolean", nullable: true),
                    is_oly_restricted = table.Column<bool>(type: "boolean", nullable: true),
                    is_sellable = table.Column<bool>(type: "boolean", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: true),
                    is_tradable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_behavior_availability", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_behavior_availability_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_behavior_availability_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_behavior_availability",
                schema: "content");

            migrationBuilder.AddColumn<bool>(
                name: "enchant_enabled",
                schema: "content",
                table: "item_weapon",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "for_npc",
                schema: "content",
                table: "item_weapon",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_effect",
                schema: "content",
                table: "item_weapon",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_depositable",
                schema: "content",
                table: "item_weapon",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_destroyable",
                schema: "content",
                table: "item_weapon",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_dropable",
                schema: "content",
                table: "item_weapon",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable",
                schema: "content",
                table: "item_weapon",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_tradable",
                schema: "content",
                table: "item_weapon",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "for_npc",
                schema: "content",
                table: "item_scroll",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_scroll",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stackable",
                schema: "content",
                table: "item_scroll",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_effect",
                schema: "content",
                table: "item_recipe",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_depositable",
                schema: "content",
                table: "item_recipe",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_destroyable",
                schema: "content",
                table: "item_recipe",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_dropable",
                schema: "content",
                table: "item_recipe",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable",
                schema: "content",
                table: "item_recipe",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stackable",
                schema: "content",
                table: "item_recipe",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_tradable",
                schema: "content",
                table: "item_recipe",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "for_npc",
                schema: "content",
                table: "item_potion",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_effect",
                schema: "content",
                table: "item_potion",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_potion",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stackable",
                schema: "content",
                table: "item_potion",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_pet_collar",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_effect",
                schema: "content",
                table: "item_material",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stackable",
                schema: "content",
                table: "item_material",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "for_npc",
                schema: "content",
                table: "item_etc",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_effect",
                schema: "content",
                table: "item_etc",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_depositable",
                schema: "content",
                table: "item_etc",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_destroyable",
                schema: "content",
                table: "item_etc",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_dropable",
                schema: "content",
                table: "item_etc",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_etc",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable",
                schema: "content",
                table: "item_etc",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stackable",
                schema: "content",
                table: "item_etc",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_tradable",
                schema: "content",
                table: "item_etc",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_effect",
                schema: "content",
                table: "item_enchant",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_oly_restricted",
                schema: "content",
                table: "item_enchant",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stackable",
                schema: "content",
                table: "item_enchant",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_effect",
                schema: "content",
                table: "item_arrow",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stackable",
                schema: "content",
                table: "item_arrow",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "enchant_enabled",
                schema: "content",
                table: "item_armor",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "for_npc",
                schema: "content",
                table: "item_armor",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_effect",
                schema: "content",
                table: "item_armor",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_depositable",
                schema: "content",
                table: "item_armor",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_destroyable",
                schema: "content",
                table: "item_armor",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_dropable",
                schema: "content",
                table: "item_armor",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable",
                schema: "content",
                table: "item_armor",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_tradable",
                schema: "content",
                table: "item_armor",
                type: "boolean",
                nullable: true);
        }
    }
}

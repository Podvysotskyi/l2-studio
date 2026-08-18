using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_recipe_types",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_recipe_types", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_item_recipe_types_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_recipes",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    item_recipe_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    craft_level = table.Column<int>(type: "integer", nullable: false),
                    success_rate = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_recipes", x => new { x.game_version, x.id });
                    table.ForeignKey(
                        name: "FK_item_recipes_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_recipes_item_recipe_types_game_version_item_recipe_typ~",
                        columns: x => new { x.game_version, x.item_recipe_type_name },
                        principalSchema: "content",
                        principalTable: "item_recipe_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_recipe_ingredients",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_recipe_id = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_recipe_ingredients", x => new { x.game_version, x.item_recipe_id, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_recipe_ingredients_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_recipe_ingredients_item_recipes_game_version_item_reci~",
                        columns: x => new { x.game_version, x.item_recipe_id },
                        principalSchema: "content",
                        principalTable: "item_recipes",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_recipe_productions",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_recipe_id = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_recipe_productions", x => new { x.game_version, x.item_recipe_id, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_recipe_productions_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_recipe_productions_item_recipes_game_version_item_reci~",
                        columns: x => new { x.game_version, x.item_recipe_id },
                        principalSchema: "content",
                        principalTable: "item_recipes",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_recipe_stat_uses",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_recipe_id = table.Column<int>(type: "integer", nullable: false),
                    mp = table.Column<int>(type: "integer", nullable: true),
                    hp = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_recipe_stat_uses", x => new { x.game_version, x.item_recipe_id });
                    table.ForeignKey(
                        name: "FK_item_recipe_stat_uses_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_recipe_stat_uses_item_recipes_game_version_item_recipe~",
                        columns: x => new { x.game_version, x.item_recipe_id },
                        principalSchema: "content",
                        principalTable: "item_recipes",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_recipes_item_recipe_type_name",
                schema: "content",
                table: "item_recipes",
                columns: new[] { "game_version", "item_recipe_type_name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_recipe_ingredients",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_recipe_productions",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_recipe_stat_uses",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_recipes",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_recipe_types",
                schema: "content");
        }
    }
}

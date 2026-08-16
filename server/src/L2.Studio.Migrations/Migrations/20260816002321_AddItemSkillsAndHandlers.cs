using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemSkillsAndHandlers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_handlers",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_handlers", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_item_handlers_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_skill_types",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_skill_types", x => new { x.game_version, x.name });
                    table.ForeignKey(
                        name: "FK_item_skill_types_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_skills",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    skill_id = table.Column<int>(type: "integer", nullable: false),
                    skill_level = table.Column<short>(type: "smallint", nullable: false),
                    item_skill_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    chance = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_skills", x => new { x.game_version, x.item_id, x.skill_id, x.skill_level });
                    table.ForeignKey(
                        name: "FK_item_skills_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_skills_item_skill_types_game_version_item_skill_type_n~",
                        columns: x => new { x.game_version, x.item_skill_type_name },
                        principalSchema: "content",
                        principalTable: "item_skill_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_skills_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_items_handler_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "handler" });

            migrationBuilder.CreateIndex(
                name: "ix_item_skills_type_name",
                schema: "content",
                table: "item_skills",
                columns: new[] { "game_version", "item_skill_type_name" });

            migrationBuilder.Sql("""
                INSERT INTO content.item_handlers (game_version, name, display_name)
                SELECT DISTINCT game_version, handler, handler
                FROM content.items
                WHERE handler IS NOT NULL
                ON CONFLICT (game_version, name) DO NOTHING;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_items_item_handlers_game_version_handler",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "handler" },
                principalSchema: "content",
                principalTable: "item_handlers",
                principalColumns: new[] { "game_version", "name" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_item_handlers_game_version_handler",
                schema: "content",
                table: "items");

            migrationBuilder.DropTable(
                name: "item_handlers",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_skills",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_skill_types",
                schema: "content");

            migrationBuilder.DropIndex(
                name: "ix_items_handler_name",
                schema: "content",
                table: "items");
        }
    }
}

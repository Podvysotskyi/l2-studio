using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_sets",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    set_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_sets", x => new { x.game_version, x.set_id });
                    table.ForeignKey(
                        name: "FK_item_sets_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_set_body_parts",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    set_id = table.Column<int>(type: "integer", nullable: false),
                    body_part_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_set_body_parts", x => new { x.game_version, x.set_id, x.body_part_name });
                    table.ForeignKey(
                        name: "FK_item_set_body_parts_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_set_body_parts_item_body_parts_game_version_body_part_~",
                        columns: x => new { x.game_version, x.body_part_name },
                        principalSchema: "content",
                        principalTable: "item_body_parts",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_set_body_parts_item_sets_game_version_set_id",
                        columns: x => new { x.game_version, x.set_id },
                        principalSchema: "content",
                        principalTable: "item_sets",
                        principalColumns: new[] { "game_version", "set_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_set_skills",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    set_id = table.Column<int>(type: "integer", nullable: false),
                    skill_id = table.Column<int>(type: "integer", nullable: false),
                    skill_level = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_set_skills", x => new { x.game_version, x.set_id, x.skill_id, x.skill_level });
                    table.ForeignKey(
                        name: "FK_item_set_skills_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_set_skills_item_sets_game_version_set_id",
                        columns: x => new { x.game_version, x.set_id },
                        principalSchema: "content",
                        principalTable: "item_sets",
                        principalColumns: new[] { "game_version", "set_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_item_set_skills_skills_game_version_skill_id",
                        columns: x => new { x.game_version, x.skill_id },
                        principalSchema: "content",
                        principalTable: "skills",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_set_stats",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    set_id = table.Column<int>(type: "integer", nullable: false),
                    str = table.Column<int>(type: "integer", nullable: true),
                    dex = table.Column<int>(type: "integer", nullable: true),
                    con = table.Column<int>(type: "integer", nullable: true),
                    @int = table.Column<int>(name: "int", type: "integer", nullable: true),
                    wit = table.Column<int>(type: "integer", nullable: true),
                    men = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_set_stats", x => new { x.game_version, x.set_id });
                    table.ForeignKey(
                        name: "FK_item_set_stats_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_set_stats_item_sets_game_version_set_id",
                        columns: x => new { x.game_version, x.set_id },
                        principalSchema: "content",
                        principalTable: "item_sets",
                        principalColumns: new[] { "game_version", "set_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_set_body_parts_game_version_body_part_name",
                schema: "content",
                table: "item_set_body_parts",
                columns: new[] { "game_version", "body_part_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_set_skills_game_version_skill_id",
                schema: "content",
                table: "item_set_skills",
                columns: new[] { "game_version", "skill_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_set_body_parts",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_set_skills",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_set_stats",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_sets",
                schema: "content");
        }
    }
}

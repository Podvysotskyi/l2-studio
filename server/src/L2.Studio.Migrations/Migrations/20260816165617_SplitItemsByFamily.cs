using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace L2.Studio.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class SplitItemsByFamily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TEMP TABLE item_family_source ON COMMIT DROP AS
                SELECT * FROM content.items;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_items_item_actions_game_version_item_action_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_items_item_body_parts_game_version_item_body_part_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_items_item_crystal_types_game_version_item_crystal_type_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_items_item_handlers_game_version_handler",
                schema: "content",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_game_version_item_action_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_game_version_item_body_part_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_game_version_item_crystal_type_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropIndex(
                name: "ix_items_handler_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "crystal_count",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "display_id",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "element_enabled",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "enchant_enabled",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "for_npc",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "handler",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "immediate_effect",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_attack_weapon",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_depositable",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_destroyable",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_dropable",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_force_equip",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_magic_weapon",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_oly_restricted",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_questitem",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_sellable",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_stackable",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "is_tradable",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "item_action_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "item_body_part_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "item_crystal_type_name",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "item_skill",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "mp_consume",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "recipe_id",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "reduced_mp_consume",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "reuse_delay",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "soulshots",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "spiritshots",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "use_condition",
                schema: "content",
                table: "items");

            migrationBuilder.DropColumn(
                name: "use_weapon_skills_only",
                schema: "content",
                table: "items");

            migrationBuilder.CreateTable(
                name: "item_armor",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_body_part_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_crystal_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    crystal_count = table.Column<int>(type: "integer", nullable: true),
                    enchant_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    for_npc = table.Column<bool>(type: "boolean", nullable: true),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_depositable = table.Column<bool>(type: "boolean", nullable: true),
                    is_destroyable = table.Column<bool>(type: "boolean", nullable: true),
                    is_dropable = table.Column<bool>(type: "boolean", nullable: true),
                    is_sellable = table.Column<bool>(type: "boolean", nullable: true),
                    is_tradable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_armor", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_armor_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_armor_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_armor_item_body_parts_game_version_item_body_part_name",
                        columns: x => new { x.game_version, x.item_body_part_name },
                        principalSchema: "content",
                        principalTable: "item_body_parts",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_armor_item_crystal_types_game_version_item_crystal_typ~",
                        columns: x => new { x.game_version, x.item_crystal_type_name },
                        principalSchema: "content",
                        principalTable: "item_crystal_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_armor_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_arrow",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_body_part_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_crystal_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_arrow", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_arrow_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_arrow_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_arrow_item_body_parts_game_version_item_body_part_name",
                        columns: x => new { x.game_version, x.item_body_part_name },
                        principalSchema: "content",
                        principalTable: "item_body_parts",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_arrow_item_crystal_types_game_version_item_crystal_typ~",
                        columns: x => new { x.game_version, x.item_crystal_type_name },
                        principalSchema: "content",
                        principalTable: "item_crystal_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_arrow_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_enchant",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    handler = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_oly_restricted = table.Column<bool>(type: "boolean", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_enchant", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_enchant_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_enchant_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_enchant_item_handlers_game_version_handler",
                        columns: x => new { x.game_version, x.handler },
                        principalSchema: "content",
                        principalTable: "item_handlers",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_enchant_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_etc",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_body_part_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_crystal_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    display_id = table.Column<int>(type: "integer", nullable: true),
                    reuse_delay = table.Column<int>(type: "integer", nullable: true),
                    handler = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_skill = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    use_condition = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    for_npc = table.Column<bool>(type: "boolean", nullable: true),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_depositable = table.Column<bool>(type: "boolean", nullable: true),
                    is_destroyable = table.Column<bool>(type: "boolean", nullable: true),
                    is_dropable = table.Column<bool>(type: "boolean", nullable: true),
                    is_oly_restricted = table.Column<bool>(type: "boolean", nullable: true),
                    is_questitem = table.Column<bool>(type: "boolean", nullable: true),
                    is_sellable = table.Column<bool>(type: "boolean", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: true),
                    is_tradable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_etc", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_etc_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_etc_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_etc_item_body_parts_game_version_item_body_part_name",
                        columns: x => new { x.game_version, x.item_body_part_name },
                        principalSchema: "content",
                        principalTable: "item_body_parts",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_etc_item_crystal_types_game_version_item_crystal_type_~",
                        columns: x => new { x.game_version, x.item_crystal_type_name },
                        principalSchema: "content",
                        principalTable: "item_crystal_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_etc_item_handlers_game_version_handler",
                        columns: x => new { x.game_version, x.handler },
                        principalSchema: "content",
                        principalTable: "item_handlers",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_etc_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_material",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_material", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_material_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_material_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_pet_collar",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    handler = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    use_condition = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_oly_restricted = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_pet_collar", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_pet_collar_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_pet_collar_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_pet_collar_item_handlers_game_version_handler",
                        columns: x => new { x.game_version, x.handler },
                        principalSchema: "content",
                        principalTable: "item_handlers",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_pet_collar_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_potion",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reuse_delay = table.Column<int>(type: "integer", nullable: true),
                    handler = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    for_npc = table.Column<bool>(type: "boolean", nullable: true),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_oly_restricted = table.Column<bool>(type: "boolean", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_potion", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_potion_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_potion_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_potion_item_handlers_game_version_handler",
                        columns: x => new { x.game_version, x.handler },
                        principalSchema: "content",
                        principalTable: "item_handlers",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_potion_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_recipe",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    recipe_id = table.Column<int>(type: "integer", nullable: true),
                    handler = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_depositable = table.Column<bool>(type: "boolean", nullable: true),
                    is_destroyable = table.Column<bool>(type: "boolean", nullable: true),
                    is_dropable = table.Column<bool>(type: "boolean", nullable: true),
                    is_sellable = table.Column<bool>(type: "boolean", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: true),
                    is_tradable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_recipe", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_recipe_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_recipe_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_recipe_item_handlers_game_version_handler",
                        columns: x => new { x.game_version, x.handler },
                        principalSchema: "content",
                        principalTable: "item_handlers",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_recipe_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_scroll",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    handler = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    for_npc = table.Column<bool>(type: "boolean", nullable: true),
                    is_oly_restricted = table.Column<bool>(type: "boolean", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_scroll", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_scroll_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_scroll_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_scroll_item_handlers_game_version_handler",
                        columns: x => new { x.game_version, x.handler },
                        principalSchema: "content",
                        principalTable: "item_handlers",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_scroll_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_weapon",
                schema: "content",
                columns: table => new
                {
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "interlude"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    item_action_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_body_part_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    item_crystal_type_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    display_id = table.Column<int>(type: "integer", nullable: true),
                    crystal_count = table.Column<int>(type: "integer", nullable: true),
                    soulshots = table.Column<int>(type: "integer", nullable: true),
                    spiritshots = table.Column<int>(type: "integer", nullable: true),
                    mp_consume = table.Column<int>(type: "integer", nullable: true),
                    reduced_mp_consume = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reuse_delay = table.Column<int>(type: "integer", nullable: true),
                    element_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    enchant_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    for_npc = table.Column<bool>(type: "boolean", nullable: true),
                    immediate_effect = table.Column<bool>(type: "boolean", nullable: true),
                    is_attack_weapon = table.Column<bool>(type: "boolean", nullable: true),
                    is_force_equip = table.Column<bool>(type: "boolean", nullable: true),
                    is_depositable = table.Column<bool>(type: "boolean", nullable: true),
                    is_destroyable = table.Column<bool>(type: "boolean", nullable: true),
                    is_dropable = table.Column<bool>(type: "boolean", nullable: true),
                    is_magic_weapon = table.Column<bool>(type: "boolean", nullable: true),
                    is_sellable = table.Column<bool>(type: "boolean", nullable: true),
                    is_tradable = table.Column<bool>(type: "boolean", nullable: true),
                    use_weapon_skills_only = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_weapon", x => new { x.game_version, x.item_id });
                    table.ForeignKey(
                        name: "FK_item_weapon_game_versions_game_version",
                        column: x => x.game_version,
                        principalSchema: "content",
                        principalTable: "game_versions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_weapon_item_actions_game_version_item_action_name",
                        columns: x => new { x.game_version, x.item_action_name },
                        principalSchema: "content",
                        principalTable: "item_actions",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_weapon_item_body_parts_game_version_item_body_part_name",
                        columns: x => new { x.game_version, x.item_body_part_name },
                        principalSchema: "content",
                        principalTable: "item_body_parts",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_weapon_item_crystal_types_game_version_item_crystal_ty~",
                        columns: x => new { x.game_version, x.item_crystal_type_name },
                        principalSchema: "content",
                        principalTable: "item_crystal_types",
                        principalColumns: new[] { "game_version", "name" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_weapon_items_game_version_item_id",
                        columns: x => new { x.game_version, x.item_id },
                        principalSchema: "content",
                        principalTable: "items",
                        principalColumns: new[] { "game_version", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_armor_game_version_item_action_name",
                schema: "content",
                table: "item_armor",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_armor_game_version_item_body_part_name",
                schema: "content",
                table: "item_armor",
                columns: new[] { "game_version", "item_body_part_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_armor_game_version_item_crystal_type_name",
                schema: "content",
                table: "item_armor",
                columns: new[] { "game_version", "item_crystal_type_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_arrow_game_version_item_action_name",
                schema: "content",
                table: "item_arrow",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_arrow_game_version_item_body_part_name",
                schema: "content",
                table: "item_arrow",
                columns: new[] { "game_version", "item_body_part_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_arrow_game_version_item_crystal_type_name",
                schema: "content",
                table: "item_arrow",
                columns: new[] { "game_version", "item_crystal_type_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_enchant_game_version_handler",
                schema: "content",
                table: "item_enchant",
                columns: new[] { "game_version", "handler" });

            migrationBuilder.CreateIndex(
                name: "IX_item_enchant_game_version_item_action_name",
                schema: "content",
                table: "item_enchant",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_etc_game_version_handler",
                schema: "content",
                table: "item_etc",
                columns: new[] { "game_version", "handler" });

            migrationBuilder.CreateIndex(
                name: "IX_item_etc_game_version_item_action_name",
                schema: "content",
                table: "item_etc",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_etc_game_version_item_body_part_name",
                schema: "content",
                table: "item_etc",
                columns: new[] { "game_version", "item_body_part_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_etc_game_version_item_crystal_type_name",
                schema: "content",
                table: "item_etc",
                columns: new[] { "game_version", "item_crystal_type_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_pet_collar_game_version_handler",
                schema: "content",
                table: "item_pet_collar",
                columns: new[] { "game_version", "handler" });

            migrationBuilder.CreateIndex(
                name: "IX_item_pet_collar_game_version_item_action_name",
                schema: "content",
                table: "item_pet_collar",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_potion_game_version_handler",
                schema: "content",
                table: "item_potion",
                columns: new[] { "game_version", "handler" });

            migrationBuilder.CreateIndex(
                name: "IX_item_potion_game_version_item_action_name",
                schema: "content",
                table: "item_potion",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_recipe_game_version_handler",
                schema: "content",
                table: "item_recipe",
                columns: new[] { "game_version", "handler" });

            migrationBuilder.CreateIndex(
                name: "IX_item_recipe_game_version_item_action_name",
                schema: "content",
                table: "item_recipe",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_scroll_game_version_handler",
                schema: "content",
                table: "item_scroll",
                columns: new[] { "game_version", "handler" });

            migrationBuilder.CreateIndex(
                name: "IX_item_scroll_game_version_item_action_name",
                schema: "content",
                table: "item_scroll",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_weapon_game_version_item_action_name",
                schema: "content",
                table: "item_weapon",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_weapon_game_version_item_body_part_name",
                schema: "content",
                table: "item_weapon",
                columns: new[] { "game_version", "item_body_part_name" });

            migrationBuilder.CreateIndex(
                name: "IX_item_weapon_game_version_item_crystal_type_name",
                schema: "content",
                table: "item_weapon",
                columns: new[] { "game_version", "item_crystal_type_name" });

            migrationBuilder.Sql("""
                INSERT INTO content.item_armor
                    (game_version, item_id, item_action_name, item_body_part_name, item_crystal_type_name,
                     crystal_count, enchant_enabled, for_npc, immediate_effect, is_depositable,
                     is_destroyable, is_dropable, is_sellable, is_tradable)
                SELECT s.game_version, s.id, s.item_action_name, s.item_body_part_name, s.item_crystal_type_name,
                       s.crystal_count, s.enchant_enabled, s.for_npc, s.immediate_effect, s.is_depositable,
                       s.is_destroyable, s.is_dropable, s.is_sellable, s.is_tradable
                FROM item_family_source s
                LEFT JOIN content.item_types t ON t.game_version = s.game_version AND t.name = s.item_type_name
                WHERE s.item_type_name = 'Armor' OR t.parent_type_name = 'Armor';

                INSERT INTO content.item_weapon
                    (game_version, item_id, item_action_name, item_body_part_name, item_crystal_type_name,
                     display_id, crystal_count, soulshots, spiritshots, mp_consume, reduced_mp_consume,
                     reuse_delay, element_enabled, enchant_enabled, for_npc, immediate_effect,
                     is_attack_weapon, is_force_equip, is_depositable, is_destroyable, is_dropable,
                     is_magic_weapon, is_sellable, is_tradable, use_weapon_skills_only)
                SELECT s.game_version, s.id, s.item_action_name, s.item_body_part_name, s.item_crystal_type_name,
                       s.display_id, s.crystal_count, s.soulshots, s.spiritshots, s.mp_consume, s.reduced_mp_consume,
                       s.reuse_delay, s.element_enabled, s.enchant_enabled, s.for_npc, s.immediate_effect,
                       s.is_attack_weapon, s.is_force_equip, s.is_depositable, s.is_destroyable, s.is_dropable,
                       s.is_magic_weapon, s.is_sellable, s.is_tradable, s.use_weapon_skills_only
                FROM item_family_source s
                LEFT JOIN content.item_types t ON t.game_version = s.game_version AND t.name = s.item_type_name
                WHERE s.item_type_name = 'Weapon' OR t.parent_type_name = 'Weapon';

                INSERT INTO content.item_arrow
                    (game_version, item_id, item_action_name, item_body_part_name, item_crystal_type_name, immediate_effect, is_stackable)
                SELECT game_version, id, item_action_name, item_body_part_name, item_crystal_type_name, immediate_effect, is_stackable
                FROM item_family_source WHERE item_type_name = 'ARROW';

                INSERT INTO content.item_material (game_version, item_id, immediate_effect, is_stackable)
                SELECT game_version, id, immediate_effect, is_stackable
                FROM item_family_source WHERE item_type_name = 'MATERIAL';

                INSERT INTO content.item_potion
                    (game_version, item_id, item_action_name, reuse_delay, handler, for_npc, immediate_effect, is_oly_restricted, is_stackable)
                SELECT game_version, id, item_action_name, reuse_delay, handler, for_npc, immediate_effect, is_oly_restricted, is_stackable
                FROM item_family_source WHERE item_type_name = 'POTION';

                INSERT INTO content.item_recipe
                    (game_version, item_id, item_action_name, recipe_id, handler, immediate_effect,
                     is_depositable, is_destroyable, is_dropable, is_sellable, is_stackable, is_tradable)
                SELECT game_version, id, item_action_name, recipe_id, handler, immediate_effect,
                       is_depositable, is_destroyable, is_dropable, is_sellable, is_stackable, is_tradable
                FROM item_family_source WHERE item_type_name = 'RECIPE';

                INSERT INTO content.item_enchant
                    (game_version, item_id, item_action_name, handler, immediate_effect, is_oly_restricted, is_stackable)
                SELECT game_version, id, item_action_name, handler, immediate_effect, is_oly_restricted, is_stackable
                FROM item_family_source WHERE item_type_name IN ('SCRL_ENCHANT_AM', 'SCRL_ENCHANT_WP');

                INSERT INTO content.item_scroll
                    (game_version, item_id, item_action_name, handler, for_npc, is_oly_restricted, is_stackable)
                SELECT game_version, id, item_action_name, handler, for_npc, is_oly_restricted, is_stackable
                FROM item_family_source WHERE item_type_name = 'SCROLL';

                INSERT INTO content.item_pet_collar
                    (game_version, item_id, item_action_name, handler, use_condition, is_oly_restricted)
                SELECT game_version, id, item_action_name, handler, use_condition, is_oly_restricted
                FROM item_family_source WHERE item_type_name = 'PET_COLLAR';

                INSERT INTO content.item_etc
                    (game_version, item_id, item_action_name, item_body_part_name, item_crystal_type_name,
                     display_id, reuse_delay, handler, item_skill, use_condition, for_npc, immediate_effect,
                     is_depositable, is_destroyable, is_dropable, is_oly_restricted, is_questitem,
                     is_sellable, is_stackable, is_tradable)
                SELECT s.game_version, s.id, s.item_action_name, s.item_body_part_name, s.item_crystal_type_name,
                       s.display_id, s.reuse_delay, s.handler, s.item_skill, s.use_condition, s.for_npc, s.immediate_effect,
                       s.is_depositable, s.is_destroyable, s.is_dropable, s.is_oly_restricted, s.is_questitem,
                       s.is_sellable, s.is_stackable, s.is_tradable
                FROM item_family_source s
                LEFT JOIN content.item_types t ON t.game_version = s.game_version AND t.name = s.item_type_name
                WHERE s.item_type_name NOT IN ('ARROW', 'MATERIAL', 'POTION', 'RECIPE', 'SCRL_ENCHANT_AM', 'SCRL_ENCHANT_WP', 'SCROLL', 'PET_COLLAR', 'Armor', 'Weapon')
                  AND COALESCE(t.parent_type_name, '') NOT IN ('Armor', 'Weapon');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TEMP TABLE item_family_restore ON COMMIT DROP AS
                SELECT i.game_version, i.id,
                    COALESCE(a.item_action_name, w.item_action_name, ar.item_action_name, p.item_action_name, r.item_action_name, e.item_action_name, s.item_action_name, pc.item_action_name, x.item_action_name) AS item_action_name,
                    COALESCE(a.item_body_part_name, w.item_body_part_name, ar.item_body_part_name, x.item_body_part_name) AS item_body_part_name,
                    COALESCE(a.item_crystal_type_name, w.item_crystal_type_name, ar.item_crystal_type_name, x.item_crystal_type_name) AS item_crystal_type_name,
                    COALESCE(w.display_id, x.display_id) AS display_id,
                    COALESCE(a.crystal_count, w.crystal_count) AS crystal_count,
                    w.soulshots, w.spiritshots, w.mp_consume, w.reduced_mp_consume,
                    COALESCE(w.reuse_delay, p.reuse_delay, x.reuse_delay) AS reuse_delay,
                    r.recipe_id,
                    COALESCE(p.handler, r.handler, e.handler, s.handler, pc.handler, x.handler) AS handler,
                    x.item_skill, COALESCE(pc.use_condition, x.use_condition) AS use_condition,
                    w.element_enabled, COALESCE(a.enchant_enabled, w.enchant_enabled) AS enchant_enabled,
                    COALESCE(a.for_npc, w.for_npc, p.for_npc, s.for_npc, x.for_npc) AS for_npc,
                    COALESCE(a.immediate_effect, w.immediate_effect, ar.immediate_effect, m.immediate_effect, p.immediate_effect, r.immediate_effect, e.immediate_effect, x.immediate_effect) AS immediate_effect,
                    w.is_attack_weapon, w.is_force_equip,
                    COALESCE(a.is_depositable, w.is_depositable, r.is_depositable, x.is_depositable) AS is_depositable,
                    COALESCE(a.is_destroyable, w.is_destroyable, r.is_destroyable, x.is_destroyable) AS is_destroyable,
                    COALESCE(a.is_dropable, w.is_dropable, r.is_dropable, x.is_dropable) AS is_dropable,
                    w.is_magic_weapon,
                    COALESCE(p.is_oly_restricted, e.is_oly_restricted, s.is_oly_restricted, pc.is_oly_restricted, x.is_oly_restricted) AS is_oly_restricted,
                    x.is_questitem,
                    COALESCE(a.is_sellable, w.is_sellable, r.is_sellable, x.is_sellable) AS is_sellable,
                    COALESCE(ar.is_stackable, m.is_stackable, p.is_stackable, r.is_stackable, e.is_stackable, s.is_stackable, x.is_stackable) AS is_stackable,
                    COALESCE(a.is_tradable, w.is_tradable, r.is_tradable, x.is_tradable) AS is_tradable,
                    w.use_weapon_skills_only
                FROM content.items i
                LEFT JOIN content.item_armor a ON a.game_version = i.game_version AND a.item_id = i.id
                LEFT JOIN content.item_weapon w ON w.game_version = i.game_version AND w.item_id = i.id
                LEFT JOIN content.item_arrow ar ON ar.game_version = i.game_version AND ar.item_id = i.id
                LEFT JOIN content.item_material m ON m.game_version = i.game_version AND m.item_id = i.id
                LEFT JOIN content.item_potion p ON p.game_version = i.game_version AND p.item_id = i.id
                LEFT JOIN content.item_recipe r ON r.game_version = i.game_version AND r.item_id = i.id
                LEFT JOIN content.item_enchant e ON e.game_version = i.game_version AND e.item_id = i.id
                LEFT JOIN content.item_scroll s ON s.game_version = i.game_version AND s.item_id = i.id
                LEFT JOIN content.item_pet_collar pc ON pc.game_version = i.game_version AND pc.item_id = i.id
                LEFT JOIN content.item_etc x ON x.game_version = i.game_version AND x.item_id = i.id;
                """);

            migrationBuilder.DropTable(
                name: "item_armor",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_arrow",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_enchant",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_etc",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_material",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_pet_collar",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_potion",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_recipe",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_scroll",
                schema: "content");

            migrationBuilder.DropTable(
                name: "item_weapon",
                schema: "content");

            migrationBuilder.AddColumn<int>(
                name: "crystal_count",
                schema: "content",
                table: "items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "display_id",
                schema: "content",
                table: "items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "element_enabled",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "enchant_enabled",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "for_npc",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handler",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_effect",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_attack_weapon",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_depositable",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_destroyable",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_dropable",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_force_equip",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_magic_weapon",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_oly_restricted",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_questitem",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stackable",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_tradable",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_action_name",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_body_part_name",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_crystal_type_name",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_skill",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mp_consume",
                schema: "content",
                table: "items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "recipe_id",
                schema: "content",
                table: "items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reduced_mp_consume",
                schema: "content",
                table: "items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reuse_delay",
                schema: "content",
                table: "items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "soulshots",
                schema: "content",
                table: "items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "spiritshots",
                schema: "content",
                table: "items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "use_condition",
                schema: "content",
                table: "items",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "use_weapon_skills_only",
                schema: "content",
                table: "items",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_game_version_item_action_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_action_name" });

            migrationBuilder.CreateIndex(
                name: "IX_items_game_version_item_body_part_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_body_part_name" });

            migrationBuilder.CreateIndex(
                name: "IX_items_game_version_item_crystal_type_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_crystal_type_name" });

            migrationBuilder.CreateIndex(
                name: "ix_items_handler_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "handler" });

            migrationBuilder.AddForeignKey(
                name: "FK_items_item_actions_game_version_item_action_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_action_name" },
                principalSchema: "content",
                principalTable: "item_actions",
                principalColumns: new[] { "game_version", "name" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_items_item_body_parts_game_version_item_body_part_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_body_part_name" },
                principalSchema: "content",
                principalTable: "item_body_parts",
                principalColumns: new[] { "game_version", "name" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_items_item_crystal_types_game_version_item_crystal_type_name",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "item_crystal_type_name" },
                principalSchema: "content",
                principalTable: "item_crystal_types",
                principalColumns: new[] { "game_version", "name" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_items_item_handlers_game_version_handler",
                schema: "content",
                table: "items",
                columns: new[] { "game_version", "handler" },
                principalSchema: "content",
                principalTable: "item_handlers",
                principalColumns: new[] { "game_version", "name" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE content.items i SET
                    item_action_name = s.item_action_name,
                    item_body_part_name = s.item_body_part_name,
                    item_crystal_type_name = s.item_crystal_type_name,
                    display_id = s.display_id,
                    crystal_count = s.crystal_count,
                    soulshots = s.soulshots,
                    spiritshots = s.spiritshots,
                    mp_consume = s.mp_consume,
                    reduced_mp_consume = s.reduced_mp_consume,
                    reuse_delay = s.reuse_delay,
                    recipe_id = s.recipe_id,
                    handler = s.handler,
                    item_skill = s.item_skill,
                    use_condition = s.use_condition,
                    element_enabled = s.element_enabled,
                    enchant_enabled = s.enchant_enabled,
                    for_npc = s.for_npc,
                    immediate_effect = s.immediate_effect,
                    is_attack_weapon = s.is_attack_weapon,
                    is_force_equip = s.is_force_equip,
                    is_depositable = s.is_depositable,
                    is_destroyable = s.is_destroyable,
                    is_dropable = s.is_dropable,
                    is_magic_weapon = s.is_magic_weapon,
                    is_oly_restricted = s.is_oly_restricted,
                    is_questitem = s.is_questitem,
                    is_sellable = s.is_sellable,
                    is_stackable = s.is_stackable,
                    is_tradable = s.is_tradable,
                    use_weapon_skills_only = s.use_weapon_skills_only
                FROM item_family_restore s
                WHERE i.game_version = s.game_version AND i.id = s.id;
                """);
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_recipe")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class Item_Recipe
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int ItemId { get; set; }
    [Column("item_action_name"), MaxLength(64)] public string? ItemActionName { get; set; }
    [Column("recipe_id")] public int? RecipeId { get; set; }
    [Column("handler"), MaxLength(64)] public string? HandlerName { get; set; }
    public Item Item { get; set; } = null!;
    public ItemAction? ItemAction { get; set; }
    public ItemHandler? ItemHandler { get; set; }
}

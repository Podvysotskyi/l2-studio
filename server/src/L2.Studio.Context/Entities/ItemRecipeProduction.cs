using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_recipe_productions")]
[PrimaryKey(nameof(GameVersion), nameof(ItemRecipeId), nameof(ItemId))]
public sealed class ItemRecipeProduction
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("item_recipe_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemRecipeId { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemId { get; set; }
    [Column("count")]
    public int Count { get; set; }
    public ItemRecipe ItemRecipe { get; set; } = null!;
}

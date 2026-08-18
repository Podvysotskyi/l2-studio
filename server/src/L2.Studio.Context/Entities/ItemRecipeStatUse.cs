using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_recipe_stat_uses")]
[PrimaryKey(nameof(GameVersion), nameof(ItemRecipeId))]
public sealed class ItemRecipeStatUse
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("item_recipe_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemRecipeId { get; set; }
    [Column("mp")]
    public int? Mp { get; set; }
    [Column("hp")]
    public int? Hp { get; set; }
    public ItemRecipe ItemRecipe { get; set; } = null!;
}

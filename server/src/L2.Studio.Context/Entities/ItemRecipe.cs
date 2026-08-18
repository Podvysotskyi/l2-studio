using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_recipes")]
[PrimaryKey(nameof(GameVersion), nameof(Id))]
public sealed class ItemRecipe
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    [Column("name"), MaxLength(100)]
    public required string Name { get; set; }
    [Column("item_recipe_type_name"), MaxLength(64)]
    public required string ItemRecipeTypeName { get; set; }
    [Column("craft_level")]
    public int CraftLevel { get; set; }
    [Column("success_rate")]
    public int SuccessRate { get; set; }
    public ItemRecipeType ItemRecipeType { get; set; } = null!;
    public ICollection<ItemRecipeIngredient> Ingredients { get; } = [];
    public ICollection<ItemRecipeProduction> Productions { get; } = [];
    public ItemRecipeStatUse? StatUse { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("items")]
[PrimaryKey(nameof(GameVersion), nameof(Id))]
public sealed class Item
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    [Column("name"), MaxLength(100)]
    public required string Name { get; set; }
    [Column("item_type_name"), MaxLength(64)]
    public required string ItemTypeName { get; set; }
    [Column("item_material_name"), MaxLength(64)]
    public string? ItemMaterialName { get; set; }
    [Column("icon"), MaxLength(256)]
    public string? Icon { get; set; }
    [Column("weight")]
    public int? Weight { get; set; }
    [Column("price")]
    public long? Price { get; set; }
    public ItemType ItemType { get; set; } = null!;
    public ItemMaterial? ItemMaterial { get; set; }
    public Item_Armor? Armor { get; set; }
    public Item_Weapon? Weapon { get; set; }
    public Item_Arrow? Arrow { get; set; }
    public Item_Material? Material { get; set; }
    public Item_Potion? Potion { get; set; }
    public Item_Recipe? Recipe { get; set; }
    public Item_Enchant? Enchant { get; set; }
    public Item_Scroll? Scroll { get; set; }
    public Item_PetCollar? PetCollar { get; set; }
    public Item_Etc? Etc { get; set; }
    public ItemBehaviorAvailability? BehaviorAvailability { get; set; }
    public ItemAttackGeometry? AttackGeometry { get; set; }
    public ICollection<ItemSkill> Skills { get; } = [];
    public ItemStats? Stats { get; set; }
}

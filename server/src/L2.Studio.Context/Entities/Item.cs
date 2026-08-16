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
    [Column("item_action_name"), MaxLength(64)]
    public string? ItemActionName { get; set; }
    [Column("item_body_part_name"), MaxLength(64)]
    public string? ItemBodyPartName { get; set; }
    [Column("item_material_name"), MaxLength(64)]
    public string? ItemMaterialName { get; set; }
    [Column("item_crystal_type_name"), MaxLength(64)]
    public string? ItemCrystalTypeName { get; set; }
    [Column("icon"), MaxLength(256)]
    public string? Icon { get; set; }
    [Column("weapon_type"), MaxLength(64)]
    public string? WeaponType { get; set; }
    [Column("armor_type"), MaxLength(64)]
    public string? ArmorType { get; set; }
    [Column("etcitem_type"), MaxLength(64)]
    public string? EtcItemType { get; set; }
    [Column("display_id")]
    public int? DisplayId { get; set; }
    [Column("crystal_count")]
    public int? CrystalCount { get; set; }
    [Column("weight")]
    public int? Weight { get; set; }
    [Column("price")]
    public long? Price { get; set; }
    [Column("soulshots")]
    public int? Soulshots { get; set; }
    [Column("spiritshots")]
    public int? Spiritshots { get; set; }
    [Column("mp_consume")]
    public int? MpConsume { get; set; }
    [Column("reduced_mp_consume"), MaxLength(64)]
    public string? ReducedMpConsume { get; set; }
    [Column("reuse_delay")]
    public int? ReuseDelay { get; set; }
    [Column("recipe_id")]
    public int? RecipeId { get; set; }
    [Column("handler"), MaxLength(64)]
    public string? HandlerName { get; set; }
    [Column("item_skill"), MaxLength(64)]
    public string? ItemSkill { get; set; }
    [Column("use_condition"), MaxLength(512)]
    public string? UseCondition { get; set; }
    [Column("element_enabled")]
    public bool? ElementEnabled { get; set; }
    [Column("enchant_enabled")]
    public bool? EnchantEnabled { get; set; }
    [Column("for_npc")]
    public bool? ForNpc { get; set; }
    [Column("immediate_effect")]
    public bool? ImmediateEffect { get; set; }
    [Column("is_attack_weapon")]
    public bool? IsAttackWeapon { get; set; }
    [Column("is_force_equip")]
    public bool? IsForceEquip { get; set; }
    [Column("is_depositable")]
    public bool? IsDepositable { get; set; }
    [Column("is_destroyable")]
    public bool? IsDestroyable { get; set; }
    [Column("is_dropable")]
    public bool? IsDropable { get; set; }
    [Column("is_magic_weapon")]
    public bool? IsMagicWeapon { get; set; }
    [Column("is_oly_restricted")]
    public bool? IsOlyRestricted { get; set; }
    [Column("is_questitem")]
    public bool? IsQuestItem { get; set; }
    [Column("is_sellable")]
    public bool? IsSellable { get; set; }
    [Column("is_stackable")]
    public bool? IsStackable { get; set; }
    [Column("is_tradable")]
    public bool? IsTradable { get; set; }
    [Column("use_weapon_skills_only")]
    public bool? UseWeaponSkillsOnly { get; set; }
    public ItemType ItemType { get; set; } = null!;
    public ItemAction? ItemAction { get; set; }
    public ItemBodyPart? ItemBodyPart { get; set; }
    public ItemMaterial? ItemMaterial { get; set; }
    public ItemCrystalType? ItemCrystalType { get; set; }
    public ItemHandler? ItemHandler { get; set; }
    public ItemAttackGeometry? AttackGeometry { get; set; }
    public ICollection<ItemSkill> Skills { get; } = [];
    public ItemStats? Stats { get; set; }
}

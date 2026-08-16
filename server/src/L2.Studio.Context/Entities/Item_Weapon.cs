using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_weapon")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class Item_Weapon
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int ItemId { get; set; }
    [Column("item_action_name"), MaxLength(64)] public string? ItemActionName { get; set; }
    [Column("item_body_part_name"), MaxLength(64)] public string? ItemBodyPartName { get; set; }
    [Column("item_crystal_type_name"), MaxLength(64)] public string? ItemCrystalTypeName { get; set; }
    [Column("display_id")] public int? DisplayId { get; set; }
    [Column("crystal_count")] public int? CrystalCount { get; set; }
    [Column("soulshots")] public int? Soulshots { get; set; }
    [Column("spiritshots")] public int? Spiritshots { get; set; }
    [Column("mp_consume")] public int? MpConsume { get; set; }
    [Column("reduced_mp_consume"), MaxLength(64)] public string? ReducedMpConsume { get; set; }
    [Column("reuse_delay")] public int? ReuseDelay { get; set; }
    [Column("element_enabled")] public bool? ElementEnabled { get; set; }
    [Column("is_attack_weapon")] public bool? IsAttackWeapon { get; set; }
    [Column("is_force_equip")] public bool? IsForceEquip { get; set; }
    [Column("is_magic_weapon")] public bool? IsMagicWeapon { get; set; }
    [Column("use_weapon_skills_only")] public bool? UseWeaponSkillsOnly { get; set; }
    public Item Item { get; set; } = null!;
    public ItemAction? ItemAction { get; set; }
    public ItemBodyPart? ItemBodyPart { get; set; }
    public ItemCrystalType? ItemCrystalType { get; set; }
}

namespace L2.Studio.Context.Entities;

public sealed class Item
{
    public required string GameVersion { get; set; }
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ItemTypeName { get; set; }
    public string? ItemActionName { get; set; }
    public string? ItemBodyPartName { get; set; }
    public string? ItemMaterialName { get; set; }
    public string? ItemCrystalTypeName { get; set; }
    public string? Icon { get; set; }
    public string? WeaponType { get; set; }
    public string? ArmorType { get; set; }
    public string? EtcItemType { get; set; }
    public string? DamageRange { get; set; }
    public int? DisplayId { get; set; }
    public int? CrystalCount { get; set; }
    public int? Weight { get; set; }
    public long? Price { get; set; }
    public int? Soulshots { get; set; }
    public int? Spiritshots { get; set; }
    public int? MpConsume { get; set; }
    public string? ReducedMpConsume { get; set; }
    public int? ReuseDelay { get; set; }
    public int? RecipeId { get; set; }
    public string? Handler { get; set; }
    public string? ItemSkill { get; set; }
    public string? UseCondition { get; set; }
    public bool? ElementEnabled { get; set; }
    public bool? EnchantEnabled { get; set; }
    public bool? ForNpc { get; set; }
    public bool? ImmediateEffect { get; set; }
    public bool? IsAttackWeapon { get; set; }
    public bool? IsForceEquip { get; set; }
    public bool? IsDepositable { get; set; }
    public bool? IsDestroyable { get; set; }
    public bool? IsDropable { get; set; }
    public bool? IsMagicWeapon { get; set; }
    public bool? IsOlyRestricted { get; set; }
    public bool? IsQuestItem { get; set; }
    public bool? IsSellable { get; set; }
    public bool? IsStackable { get; set; }
    public bool? IsTradable { get; set; }
    public bool? UseWeaponSkillsOnly { get; set; }
    public ItemType ItemType { get; set; } = null!;
    public ItemAction? ItemAction { get; set; }
    public ItemBodyPart? ItemBodyPart { get; set; }
    public ItemMaterial? ItemMaterial { get; set; }
    public ItemCrystalType? ItemCrystalType { get; set; }
    public ItemStats? Stats { get; set; }
}

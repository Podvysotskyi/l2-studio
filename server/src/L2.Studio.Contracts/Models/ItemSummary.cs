namespace L2.Studio.Contracts;

public sealed record ItemSummary(
    int Id, string Name, string ItemTypeName, string ItemTypeDisplayName, string? ItemActionName,
    string? ItemActionDisplayName, string? ItemBodyPartName, string? ItemBodyPartDisplayName,
    string? ItemMaterialName, string? ItemMaterialDisplayName, string? ItemCrystalTypeName,
    string? ItemCrystalTypeDisplayName, string? Icon, int? Weight, long? Price, string? WeaponType,
    string? ArmorType, string? EtcItemType, string? DamageRange, ItemStatsSummary? Stats);

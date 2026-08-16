namespace L2.Studio.Contracts;

public sealed record ItemSummary(
    int Id, string Name, string ItemTypeName, string ItemTypeDisplayName, string? ItemParentTypeName,
    string? ItemParentTypeDisplayName, string? ItemActionName,
    string? ItemActionDisplayName, string? ItemBodyPartName, string? ItemBodyPartDisplayName,
    string? ItemMaterialName, string? ItemMaterialDisplayName, string? ItemCrystalTypeName,
    string? ItemCrystalTypeDisplayName, string? Icon, int? Weight, long? Price, string? HandlerName,
    string? HandlerDisplayName,
    IReadOnlyList<ItemSkillSummary> Skills, ItemAttackGeometrySummary? AttackGeometry, ItemStatsSummary? Stats);

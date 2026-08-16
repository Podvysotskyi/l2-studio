namespace L2.Studio.Contracts.Requests;

public sealed record UpdateItemRequest(
    string? Name,
    string? ItemTypeName,
    string? ItemActionName,
    string? ItemBodyPartName,
    string? ItemMaterialName,
    string? ItemCrystalTypeName,
    string? Icon,
    int? Weight,
    long? Price,
    string? WeaponType,
    string? ArmorType,
    string? EtcItemType,
    string? HandlerName,
    UpdateItemAttackGeometryRequest? AttackGeometry);

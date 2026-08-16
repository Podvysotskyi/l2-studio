namespace L2.Studio.Contracts.Requests;

public sealed record UpdateItemRequest(
    string? Name,
    string? ItemActionName,
    string? ItemBodyPartName,
    string? ItemMaterialName,
    string? ItemCrystalTypeName,
    string? Icon,
    int? Weight,
    long? Price,
    string? HandlerName,
    UpdateItemAttackGeometryRequest? AttackGeometry);

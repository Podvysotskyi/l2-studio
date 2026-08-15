namespace L2.Studio.Contracts.Requests;

public sealed record UpdatePlayerClassRequest(string? Name, bool IsMage, int? ParentClassId);

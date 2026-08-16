namespace L2.Studio.Contracts.Requests;

public sealed record UpdateItemSkillRequest(
    string? ItemSkillTypeName,
    int? Chance);

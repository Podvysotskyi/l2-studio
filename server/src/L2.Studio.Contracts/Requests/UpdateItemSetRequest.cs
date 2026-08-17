namespace L2.Studio.Contracts.Requests;

public sealed record UpdateItemSetRequest(
    int SkillId,
    short SkillLevel,
    int? Str,
    int? Dex,
    int? Con,
    int? Int,
    int? Wit,
    int? Men);

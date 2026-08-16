namespace L2.Studio.Contracts.Requests;

public sealed record SetItemPrimarySkillRequest(
    int SkillId,
    short SkillLevel);

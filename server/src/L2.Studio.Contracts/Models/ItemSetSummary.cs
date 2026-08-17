namespace L2.Studio.Contracts;

public sealed record ItemSetSummary(
    int SetId,
    IReadOnlyList<ItemSetBodyPartSummary> BodyParts,
    ItemSetSkillSummary? Skill,
    ItemSetStatsSummary? Stats);

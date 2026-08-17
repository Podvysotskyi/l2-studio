namespace L2.Studio.Worker;

public sealed record ItemSetDefinition(
    int SetId,
    IReadOnlyList<ItemSetBodyPartDefinition> BodyParts,
    ItemSetSkillDefinition Skill,
    ItemSetStatsDefinition? Stats);

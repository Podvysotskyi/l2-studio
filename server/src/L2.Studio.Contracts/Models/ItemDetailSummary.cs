namespace L2.Studio.Contracts;

public sealed record ItemDetailSummary(
    ItemSummary Item,
    ItemPropertiesSummary Properties,
    ItemPrimarySkillSummary? PrimarySkill);

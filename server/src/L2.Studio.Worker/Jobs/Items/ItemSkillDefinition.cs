namespace L2.Studio.Worker;

public sealed record ItemSkillDefinition(
    int SkillId,
    short SkillLevel,
    string? TypeName,
    int? Chance);

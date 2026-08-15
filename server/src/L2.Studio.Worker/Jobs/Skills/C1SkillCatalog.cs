namespace L2.Studio.Worker;

public sealed partial class C1SkillCatalog
{
    private static readonly SkillOperateTypeDefinition[] OperateTypeDefinitions =
    [
        new("A1", "A1"), new("A2", "A2"), new("A3", "A3"),
        new("CA1", "CA1"), new("CA5", "CA5"), new("P", "P"), new("T", "T")
    ];

    private static readonly SkillTargetTypeDefinition[] TargetTypeDefinitions =
    [
        new("AREA", "Area"), new("AREA_CORPSE_MOB", "Area Corpse Mob"),
        new("AREA_SUMMON", "Area Summon"), new("AURA", "Aura"),
        new("AURA_CORPSE_MOB", "Aura Corpse Mob"), new("BEHIND_AURA", "Behind Aura"),
        new("CLAN", "Clan"), new("CLAN_MEMBER", "Clan Member"),
        new("CORPSE", "Corpse"), new("CORPSE_CLAN", "Corpse Clan"),
        new("CORPSE_MOB", "Corpse Mob"), new("ENEMY_SUMMON", "Enemy Summon"),
        new("FRONT_AREA", "Front Area"), new("FRONT_AURA", "Front Aura"),
        new("GROUND", "Ground"), new("HOLY", "Holy"), new("NONE", "None"), new("ONE", "One"),
        new("OWNER_PET", "Owner Pet"), new("PARTY", "Party"), new("PARTY_CLAN", "Party Clan"),
        new("PARTY_MEMBER", "Party Member"), new("PARTY_NOT_ME", "Party Not Me"), new("PC_BODY", "Pc Body"),
        new("SELF", "Self"), new("SERVITOR", "Servitor"), new("UNLOCKABLE", "Unlockable")
    ];

    public IReadOnlyList<SkillDefinition> Skills => Definitions;
    public IReadOnlyList<SkillOperateTypeDefinition> OperateTypes => OperateTypeDefinitions;
    public IReadOnlyList<SkillTargetTypeDefinition> TargetTypes => TargetTypeDefinitions;
}

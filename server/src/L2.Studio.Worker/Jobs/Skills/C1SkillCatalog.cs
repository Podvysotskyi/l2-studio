using L2.Studio.Context.Identifiers;

namespace L2.Studio.Worker;

public sealed partial class C1SkillCatalog
{
    private static readonly SkillOperateTypeDefinition[] OperateTypeDefinitions =
    [
        new(SkillOperateTypeId.A1, "A1"), new(SkillOperateTypeId.A2, "A2"),
        new(SkillOperateTypeId.A3, "A3"), new(SkillOperateTypeId.CA1, "CA1"),
        new(SkillOperateTypeId.CA5, "CA5"), new(SkillOperateTypeId.P, "P"),
        new(SkillOperateTypeId.T, "T")
    ];

    private static readonly SkillTargetTypeDefinition[] TargetTypeDefinitions =
    [
        new(SkillTargetTypeId.Area, "AREA"), new(SkillTargetTypeId.AreaCorpseMob, "AREA_CORPSE_MOB"),
        new(SkillTargetTypeId.AreaSummon, "AREA_SUMMON"), new(SkillTargetTypeId.Aura, "AURA"),
        new(SkillTargetTypeId.AuraCorpseMob, "AURA_CORPSE_MOB"), new(SkillTargetTypeId.BehindAura, "BEHIND_AURA"),
        new(SkillTargetTypeId.Clan, "CLAN"), new(SkillTargetTypeId.ClanMember, "CLAN_MEMBER"),
        new(SkillTargetTypeId.Corpse, "CORPSE"), new(SkillTargetTypeId.CorpseClan, "CORPSE_CLAN"),
        new(SkillTargetTypeId.CorpseMob, "CORPSE_MOB"), new(SkillTargetTypeId.EnemySummon, "ENEMY_SUMMON"),
        new(SkillTargetTypeId.FrontArea, "FRONT_AREA"), new(SkillTargetTypeId.FrontAura, "FRONT_AURA"),
        new(SkillTargetTypeId.Ground, "GROUND"), new(SkillTargetTypeId.Holy, "HOLY"),
        new(SkillTargetTypeId.None, "NONE"), new(SkillTargetTypeId.One, "ONE"),
        new(SkillTargetTypeId.OwnerPet, "OWNER_PET"), new(SkillTargetTypeId.Party, "PARTY"),
        new(SkillTargetTypeId.PartyClan, "PARTY_CLAN"), new(SkillTargetTypeId.PartyMember, "PARTY_MEMBER"),
        new(SkillTargetTypeId.PartyNotMe, "PARTY_NOT_ME"), new(SkillTargetTypeId.PcBody, "PC_BODY"),
        new(SkillTargetTypeId.Self, "SELF"), new(SkillTargetTypeId.Servitor, "SERVITOR"),
        new(SkillTargetTypeId.Unlockable, "UNLOCKABLE")
    ];

    public IReadOnlyList<SkillDefinition> Skills => Definitions;
    public IReadOnlyList<SkillOperateTypeDefinition> OperateTypes => OperateTypeDefinitions;
    public IReadOnlyList<SkillTargetTypeDefinition> TargetTypes => TargetTypeDefinitions;
}

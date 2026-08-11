using L2.Studio.Context.Identifiers;
using L2.Studio.Migrations.Seeding;
using Xunit;

namespace L2.Studio.Migrations.Tests;

public sealed class MigrationSeedValuesTests
{
    [Fact]
    public void CoversEveryPlayerRaceAndSexLookupExactlyOnce()
    {
        Assert.Equal(
            Enum.GetValues<PlayerRaceId>(),
            PlayerLookupSeedValues.Races.Select(item => item.Id));
        Assert.Equal(
            Enum.GetValues<PlayerSexId>(),
            PlayerLookupSeedValues.Sexes.Select(item => item.Id));
        Assert.Equal(
            PlayerLookupSeedValues.Races.Count,
            PlayerLookupSeedValues.Races.Select(item => item.Name).Distinct().Count());
        Assert.Equal(
            PlayerLookupSeedValues.Sexes.Count,
            PlayerLookupSeedValues.Sexes.Select(item => item.Name).Distinct().Count());
    }

    [Fact]
    public void CoversEveryNpcLookupIdentifierExactlyOnce()
    {
        Assert.Equal(
            Enum.GetValues<NpcTypeId>(),
            NpcLookupSeedValues.Types.Select(item => item.Id));
        Assert.Equal(
            Enum.GetValues<NpcRaceId>(),
            NpcLookupSeedValues.Races.Select(item => item.Id));
        Assert.Equal(
            Enum.GetValues<NpcSexId>(),
            NpcLookupSeedValues.Sexes.Select(item => item.Id));
    }

    [Fact]
    public void DefinesEveryPlayerClassWithAValidParentAndCanonicalAvailability()
    {
        var classes = PlayerClassSeedValues.PlayerClasses;
        var ids = classes.Select(item => item.Id).ToHashSet();

        Assert.Equal(Enum.GetValues<PlayerClassId>().Length, classes.Count);
        Assert.Equal(classes.Count, ids.Count);
        Assert.All(classes, item =>
        {
            if (item.ParentClassId is not null) Assert.Contains(item.ParentClassId.Value, ids);
            var availability = Assert.Single(item.AllowedRaces);
            Assert.Equal(
                [PlayerSexId.Male, PlayerSexId.Female],
                availability.AllowedSexIds);
        });
    }

    [Fact]
    public void BuildsAppearanceOptionsForEveryRaceAndSexCombination()
    {
        var combinations = Enum.GetValues<PlayerRaceId>().Length *
            Enum.GetValues<PlayerSexId>().Length;

        Assert.Equal(combinations * 3, PlayerAppearanceSeedValues.Faces.Count);
        Assert.Equal(combinations / 2 * (5 + 7), PlayerAppearanceSeedValues.HairStyles.Count);
        Assert.Equal(combinations * 4, PlayerAppearanceSeedValues.HairColors.Count);
        Assert.All(
            PlayerAppearanceSeedValues.Faces
                .Concat(PlayerAppearanceSeedValues.HairStyles)
                .Concat(PlayerAppearanceSeedValues.HairColors),
            option => Assert.False(string.IsNullOrWhiteSpace(option.Name)));
    }

    [Fact]
    public void KeepsGeneratedNpcAndSkillKeysUniqueAndReferencesValidLookups()
    {
        Assert.Equal(
            NpcSeedValues.Npcs.Count,
            NpcSeedValues.Npcs.Select(item => item.Id).Distinct().Count());
        Assert.All(NpcSeedValues.Npcs, npc =>
        {
            Assert.True(Enum.IsDefined(npc.NpcTypeId));
            Assert.True(npc.NpcRaceId is null || Enum.IsDefined(npc.NpcRaceId.Value));
            Assert.True(Enum.IsDefined(npc.NpcSexId));
            Assert.InRange(npc.Level, (short)1, (short)255);
        });

        Assert.Equal(
            SkillSeedValues.Skills.Count,
            SkillSeedValues.Skills.Select(item => item.Id).Distinct().Count());
        Assert.All(SkillSeedValues.Skills, skill =>
        {
            Assert.InRange(skill.Levels, (short)1, (short)255);
            Assert.True(skill.SkillOperateTypeId is null ||
                Enum.IsDefined(skill.SkillOperateTypeId.Value));
            Assert.True(skill.SkillTargetTypeId is null ||
                Enum.IsDefined(skill.SkillTargetTypeId.Value));
        });
    }
}

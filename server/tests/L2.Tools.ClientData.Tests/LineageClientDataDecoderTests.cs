using System.Text;
using L2.Tools.ClientData;
using Xunit;

namespace L2.Tools.ClientData.Tests;

public sealed class LineageClientDataDecoderTests
{
    [Fact]
    public void DecodesProtocol211Payload()
    {
        var payload = Enumerable.Range(0, 35).Select(index => (byte)index).ToArray();
        var encoded = LineageClientDataDecoder.EncodeProtocol211ForTests(payload);

        Assert.Equal(payload, LineageClientDataDecoder.DecodeProtocol211(encoded));
    }

    [Fact]
    public void ReadsNpcAppearanceRecord()
    {
        const string source = "npc_begin\tnpc_id=1001\tnpc_name=[Goblin Scout]\tnpc_speed=1.5\t" +
            "class_name=[LineageMonster.goblin]\tmesh_name=[LineageMonsters.goblin_m00]\t" +
            "texture_name={[LineageMonstersTex.goblin_t00];[LineageMonstersTex.goblin_t01]}\t" +
            "collision_radius=10\tcollision_height=15\tattack_sound1={[ItemSound.club_1]}\t" +
            "defense_sound1={[MonSound.Hit_Wet_4]}\tdamage_sound={[MonSound.goblin_dmg_1]}\t" +
            "sound_vol=250\tsound_radius=50\tsound_random=70\t" +
            "attack_effect=[LineageEffect.p_u002_a]\tnpc_end\r\n";

        var record = Assert.Single(NpcGrpReader.ReadDecoded(Encoding.Unicode.GetBytes(source)));

        Assert.Equal((uint)1001, record.Id);
        Assert.Equal("Goblin Scout", record.Name);
        Assert.Equal("LineageMonster.goblin", record.ClassName);
        Assert.Equal("LineageMonsters.goblin_m00", record.Mesh);
        Assert.Equal(["LineageMonstersTex.goblin_t00", "LineageMonstersTex.goblin_t01"], record.Textures);
        Assert.Equal(["ItemSound.club_1"], record.AttackSounds);
        Assert.Equal(["MonSound.Hit_Wet_4"], record.DefenceSounds);
        Assert.Equal(["MonSound.goblin_dmg_1"], record.DamageSounds);
        Assert.Equal("LineageEffect.p_u002_a", record.AttackEffect);
    }

    [Fact]
    public void RemovesNoneSoundReferences()
    {
        const string source = "npc_begin npc_id=1 npc_name=[gremlin] npc_speed=1 class_name=[monster] mesh_name=[mesh] " +
            "texture_name={[texture]} collision_radius=10 collision_height=15 attack_sound1={[none]} " +
            "defense_sound1={[none]} damage_sound={[none]} sound_vol=250 sound_radius=50 sound_random=70 " +
            "attack_effect=[effect] npc_end";

        var record = Assert.Single(NpcGrpReader.ReadDecoded(Encoding.Unicode.GetBytes(source)));

        Assert.Empty(record.AttackSounds);
        Assert.Empty(record.DefenceSounds);
        Assert.Empty(record.DamageSounds);
    }

    [Theory]
    [InlineData("{[texture_a]:[texture_b]}")]
    [InlineData("{[texture_a,texture_b]}")]
    [InlineData("{[texture_a];[texture_b]}")]
    public void ReadsNpcAppearanceArraySeparatorVariants(string textureNames)
    {
        var source = "npc_begin npc_id=1 npc_name=[gremlin] npc_speed=1 class_name=[monster] mesh_name=[mesh] " +
            $"texture_name={textureNames} collision_radius=10 collision_height=15 attack_sound1={{[none]}} " +
            "defense_sound1={[none]} damage_sound={[none]} sound_vol=250 sound_radius=50 sound_random=70 " +
            "attack_effect=[effect] npc_end";

        var record = Assert.Single(NpcGrpReader.ReadDecoded(Encoding.Unicode.GetBytes(source)));

        Assert.Equal(["texture_a", "texture_b"], record.Textures);
    }

    [Fact]
    public void ReadsOptionalLocalChronicleOneFixture()
    {
        var path = Environment.GetEnvironmentVariable("L2_C1_NPCGRP_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;

        var records = NpcGrpReader.ReadProtocol211(File.ReadAllBytes(path));

        Assert.NotEmpty(records);
        Assert.All(records, record => Assert.NotEqual((uint)0, record.Id));
        Assert.Contains(records, record => !string.IsNullOrWhiteSpace(record.Mesh));
    }
}

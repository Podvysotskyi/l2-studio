using System.Text;
using System.Text.Json;
using L2.Tools.StaticMeshConverter;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class ManifestSerializationTests
{
    [Fact]
    public void SerializesPublishedManifestsAsCompactJsonWithATrailingNewline()
    {
        var contents = AssetImportJobProcessor.SerializeManifest(new
        {
            SchemaVersion = 1,
            Entries = new[]
            {
                new { Name = "First", Value = 1 },
                new { Name = "Second", Value = 2 }
            }
        });

        var json = Encoding.UTF8.GetString(contents);
        Assert.EndsWith("\n", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\n ", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(contents.AsMemory(0, contents.Length - 1));
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(2, document.RootElement.GetProperty("entries").GetArrayLength());
    }

    [Fact]
    public void SerializesMapLevelSummaryMetadataWhenPresent()
    {
        var contents = AssetImportJobProcessor.SerializeManifest(new MapManifest(
            16,
            "17_25",
            "17_25.unr",
            "source-hash",
            111,
            new MapLevelSummaryManifestEntry(
                "Talking Island",
                "L2 Studio",
                "A starting area.",
                "Welcome.",
                null,
                null,
                false,
                2,
                8,
                null,
                "MyLevel.Screenshot"),
            new MapEnvironmentManifestEntry(new MapColor(0, 0, 0), 0, null),
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            new Dictionary<string, int>(),
            []));

        using var document = JsonDocument.Parse(contents.AsMemory(0, contents.Length - 1));
        var summary = document.RootElement.GetProperty("summary");
        Assert.Equal("Talking Island", summary.GetProperty("title").GetString());
        Assert.False(summary.GetProperty("hideFromMenus").GetBoolean());
        Assert.Equal("MyLevel.Screenshot", summary.GetProperty("screenshot").GetString());
    }

    [Fact]
    public void SerializesPlayerStartLocations()
    {
        var contents = AssetImportJobProcessor.SerializeManifest(new MapManifest(
            16,
            "17_25",
            "17_25.unr",
            "source-hash",
            111,
            null,
            new MapEnvironmentManifestEntry(new MapColor(0, 0, 0), 0, null),
            [],
            [],
            [new MapPlayerStartManifestEntry(
                "PlayerStart0",
                new MapVector(1, 2, 3),
                new MapRotation(4, 5, 6))],
            [],
            [],
            [],
            [],
            new Dictionary<string, int>(),
            []));

        using var document = JsonDocument.Parse(contents.AsMemory(0, contents.Length - 1));
        var playerStart = document.RootElement.GetProperty("playerStarts")[0];
        Assert.Equal("PlayerStart0", playerStart.GetProperty("name").GetString());
        Assert.Equal(1, playerStart.GetProperty("location").GetProperty("x").GetSingle());
        Assert.Equal(5, playerStart.GetProperty("rotation").GetProperty("yaw").GetInt32());
    }

    [Fact]
    public void SerializesANullMapLevelSummaryWhenItIsUnavailable()
    {
        var contents = AssetImportJobProcessor.SerializeManifest(new { Summary = (object?)null });

        using var document = JsonDocument.Parse(contents.AsMemory(0, contents.Length - 1));
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("summary").ValueKind);
    }

    [Fact]
    public void SerializesNormalizedNpcAppearanceReferences()
    {
        var materialReference = new NpcMaterialReference(
            "LineageMonstersTex.goblin_t00",
            "/textures/goblin.webp",
            new StaticMeshMaterialBinding(
                "goblin_t00",
                "/textures/goblin.webp",
                null,
                null,
                StaticMeshBlendMode.AlphaBlend,
                true,
                0.5f,
                true,
                true,
                StaticMeshOpacitySource.Texture,
                StaticMeshOpacityChannel.Alpha));
        var manifest = new NpcAppearanceManifest(
            6,
            "npcappearances",
            "system/npcgrp.txt",
            "source-hash",
            211,
            new NpcAppearanceManifestEntry(
                20003,
                3,
                "goblin",
                1,
                "LineageMonster.goblin",
                new NpcAnimationAssetReference(
                    "LineageMonsters.goblin_m00",
                    "/animations/goblin.glb",
                    "/animations/goblin.animations.glb"),
                [materialReference],
                [new NpcAppearanceMaterialSlot(0, materialReference, materialReference, materialReference, "override", null)],
                10,
                15,
                [],
                [],
                [],
                250,
                50,
                70,
                new NpcAssetReference("LineageEffect.p_u002_a", null)));

        using var document = JsonDocument.Parse(AssetImportJobProcessor.SerializeNpcAppearanceManifest(manifest));
        var json = document.RootElement;
        var npc = json.GetProperty("npc");
        Assert.Equal(6, json.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(20003, npc.GetProperty("id").GetInt32());
        Assert.Equal((uint)3, npc.GetProperty("appearanceId").GetUInt32());
        Assert.Equal("goblin", npc.GetProperty("appearanceName").GetString());
        Assert.Equal("/animations/goblin.glb", npc.GetProperty("mesh").GetProperty("url").GetString());
        Assert.Equal("/animations/goblin.animations.glb", npc.GetProperty("mesh").GetProperty("animationUrl").GetString());
        var texture = npc.GetProperty("textures")[0];
        Assert.Equal("/textures/goblin.webp", texture.GetProperty("url").GetString());
        Assert.Equal("alphablend", texture.GetProperty("material").GetProperty("blendMode").GetString());
        Assert.Equal("texture", texture.GetProperty("material").GetProperty("opacitySource").GetString());
        Assert.Equal("alpha", texture.GetProperty("material").GetProperty("opacityChannel").GetString());
        Assert.Equal("override", npc.GetProperty("materialSlots")[0].GetProperty("effectiveSource").GetString());
        Assert.Equal("LineageEffect.p_u002_a", npc.GetProperty("attackEffect").GetProperty("reference").GetString());
    }

    [Fact]
    public void UsesOneStableManifestPathPerNpc() =>
        Assert.Equal("npcs/3/manifest.json", AssetImportJobProcessor.NpcAppearanceManifestRelativePath(3));

    [Fact]
    public void PublishesSeparateNpcManifestsForSharedClientAppearances()
    {
        var appearance = Appearance(1);

        var matches = AssetImportJobProcessor.MatchNpcAppearances(
            [appearance],
            [(20001, 1), (900001, 1), (20775, 775), (80000, null)]);

        Assert.Equal([20001, 900001], matches.Select(match => match.Id));
        Assert.All(matches, match => Assert.Equal((uint)1, match.AppearanceId));
    }

    [Fact]
    public void ComposesResolvedOverridesAndFallsBackToSkeletalDefaults()
    {
        var defaultBody = MaterialReference("default-body");
        var defaultArmor = MaterialReference("default-armor");
        var bodyOverride = MaterialReference("body-override");
        var missingArmorOverride = MaterialReference("missing-armor", resolved: false);

        var slots = AssetImportJobProcessor.ComposeNpcMaterialSlots(
            [defaultBody, defaultArmor],
            [bodyOverride, missingArmorOverride]);

        Assert.Equal("override", slots[0].EffectiveSource);
        Assert.Same(bodyOverride, slots[0].EffectiveMaterial);
        Assert.Null(slots[0].Warning);
        Assert.Equal("default", slots[1].EffectiveSource);
        Assert.Same(defaultArmor, slots[1].EffectiveMaterial);
        Assert.Contains("using the mesh default", slots[1].Warning);
    }

    private static NpcAppearanceManifestEntry Appearance(uint appearanceId) => new(
        checked((int)appearanceId),
        appearanceId,
        "appearance",
        1,
        "LineageMonster.appearance",
        new NpcAnimationAssetReference("mesh", "/mesh.glb", "/animations.glb"),
        [],
        [],
        10,
        15,
        [],
        [],
        [],
        250,
        50,
        70,
        new NpcAssetReference("effect", null));

    private static NpcMaterialReference MaterialReference(string name, bool resolved = true) => new(
        name,
        resolved ? $"/{name}.webp" : null,
        resolved
            ? new StaticMeshMaterialBinding(
                name, $"/{name}.webp", null, null, StaticMeshBlendMode.Opaque,
                false, 0.5f, true, true)
            : null);
}

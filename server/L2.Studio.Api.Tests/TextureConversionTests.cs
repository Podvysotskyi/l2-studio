using System.Buffers.Binary;
using L2.Tools.PackageReader;
using L2.Tools.TextureConverter;
using L2.Tools.StaticMeshConverter;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace L2.Foundation.Tests;

public sealed class TextureConversionTests
{
    [Theory]
    [InlineData(UnrealTextureFormat.Dxt1, 0x83f1u, 8)]
    [InlineData(UnrealTextureFormat.Dxt3, 0x83f2u, 16)]
    [InlineData(UnrealTextureFormat.Dxt5, 0x83f3u, 16)]
    public void Ktx_preserves_native_dxt_mip_payloads(
        UnrealTextureFormat format,
        uint expectedInternalFormat,
        int blockSize)
    {
        var baseData = Enumerable.Repeat((byte)0x11, blockSize * 4).ToArray();
        var secondData = Enumerable.Repeat((byte)0x22, blockSize).ToArray();
        var texture = new UnrealTexture(
            "native-mips",
            format,
            8,
            8,
            baseData,
            Mips:
            [
                new UnrealTextureMip(8, 8, baseData),
                new UnrealTextureMip(4, 4, secondData)
            ]);

        var encoded = KtxTextureEncoder.Encode(texture);

        Assert.Equal(
            new byte[] { 0xab, 0x4b, 0x54, 0x58, 0x20, 0x31, 0x31, 0xbb, 0x0d, 0x0a, 0x1a, 0x0a },
            encoded[..12]);
        Assert.Equal(expectedInternalFormat, ReadUInt32(encoded, 28));
        Assert.Equal(8u, ReadUInt32(encoded, 36));
        Assert.Equal(8u, ReadUInt32(encoded, 40));
        Assert.Equal(2u, ReadUInt32(encoded, 56));
        Assert.Equal((uint)baseData.Length, ReadUInt32(encoded, 64));
        Assert.Equal(baseData, encoded.AsSpan(68, baseData.Length).ToArray());
        var secondSizeOffset = 68 + baseData.Length;
        Assert.Equal((uint)secondData.Length, ReadUInt32(encoded, secondSizeOffset));
        Assert.Equal(secondData, encoded.AsSpan(secondSizeOffset + 4, secondData.Length).ToArray());
    }

    [Fact]
    public void Ktx_rejects_non_halving_mip_dimensions()
    {
        var texture = new UnrealTexture(
            "bad-mips",
            UnrealTextureFormat.Dxt1,
            8,
            8,
            new byte[32],
            Mips:
            [
                new UnrealTextureMip(8, 8, new byte[32]),
                new UnrealTextureMip(2, 2, new byte[8])
            ]);

        var error = Assert.Throws<InvalidDataException>(() => KtxTextureEncoder.Encode(texture));

        Assert.Contains("4x4 were expected", error.Message);
    }

    [Fact]
    public void Dxt1_decodes_rgb565_palette()
    {
        var data = new byte[]
        {
            0x00, 0xf8, // red
            0xe0, 0x07, // green
            0x00, 0x00, 0x00, 0x00
        };
        var pixels = DxtDecoder.Decode(new UnrealTexture("red", UnrealTextureFormat.Dxt1, 4, 4, data));

        Assert.All(pixels, pixel =>
        {
            Assert.Equal(255, pixel.R);
            Assert.Equal(0, pixel.G);
            Assert.Equal(0, pixel.B);
            Assert.Equal(255, pixel.A);
        });
    }

    [Fact]
    public void Dxt3_expands_four_bit_alpha()
    {
        var data = new byte[]
        {
            0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xff, 0xff, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };
        var pixels = DxtDecoder.Decode(new UnrealTexture("alpha", UnrealTextureFormat.Dxt3, 4, 4, data));

        Assert.Equal(255, pixels[0].A);
        Assert.Equal(0, pixels[1].A);
    }

    [Fact]
    public void Dxt5_decodes_interpolated_alpha_palette()
    {
        var data = new byte[]
        {
            255, 0,
            0, 0, 0, 0, 0, 0,
            0xff, 0xff, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };
        var pixels = DxtDecoder.Decode(new UnrealTexture("alpha", UnrealTextureFormat.Dxt5, 4, 4, data));

        Assert.All(pixels, pixel => Assert.Equal(255, pixel.A));
    }

    [Fact]
    public void Rgba8_decodes_unreal_bgra_byte_order()
    {
        var pixels = DxtDecoder.Decode(new UnrealTexture(
            "color",
            UnrealTextureFormat.Rgba8,
            1,
            1,
            [30, 20, 10, 40]));

        Assert.Equal((byte)10, pixels[0].R);
        Assert.Equal((byte)20, pixels[0].G);
        Assert.Equal((byte)30, pixels[0].B);
        Assert.Equal((byte)40, pixels[0].A);
    }

    [Fact]
    public void P8_resolves_palette_indices()
    {
        var palette = new[]
        {
            new UnrealColor(1, 2, 3, 4),
            new UnrealColor(10, 20, 30, 40)
        };
        var pixels = DxtDecoder.Decode(new UnrealTexture(
            "palette",
            UnrealTextureFormat.P8,
            2,
            1,
            [1, 0],
            palette));

        Assert.Equal(new Rgba32(10, 20, 30, 40), pixels[0]);
        Assert.Equal(new Rgba32(1, 2, 3, 4), pixels[1]);
    }

    [Fact]
    public void G16_decodes_little_endian_values_as_grayscale_preview()
    {
        var pixels = DxtDecoder.Decode(new UnrealTexture(
            "height",
            UnrealTextureFormat.G16,
            2,
            1,
            [0x34, 0x12, 0xcd, 0xab]));

        Assert.Equal(new Rgba32(0x12, 0x12, 0x12, 255), pixels[0]);
        Assert.Equal(new Rgba32(0xab, 0xab, 0xab, 255), pixels[1]);
    }

    [Fact]
    public void Terrain_alpha_maps_are_packed_into_rgba_channels_in_layer_order()
    {
        var alphaMaps = new byte[] { 10, 20, 30, 40, 50 }
            .Select(value => new UnrealTexture(
                $"alpha-{value}",
                UnrealTextureFormat.P8,
                1,
                1,
                [0],
                [new UnrealColor(value, 0, 0, 255)]))
            .ToArray();

        var controlMaps = TerrainControlMapEncoder.Pack(alphaMaps);

        Assert.Equal(2, controlMaps.Count);
        Assert.Equal([0, 1, 2, 3], controlMaps[0].LayerIndices);
        Assert.Equal(new Rgba32(10, 20, 30, 40), controlMaps[0].Pixels[0]);
        Assert.Equal([4], controlMaps[1].LayerIndices);
        Assert.Equal(new Rgba32(50, 0, 0, 0), controlMaps[1].Pixels[0]);
    }

    [Fact]
    public async Task Terrain_control_map_transport_preserves_four_independent_weights()
    {
        var packed = new PackedTerrainControlMap(
            2,
            1,
            [0, 1, 2, 3],
            [new Rgba32(10, 20, 30, 0), new Rgba32(40, 50, 60, 128)]);

        var transport = TerrainControlMapEncoder.EncodeOpaqueTransport(packed);

        Assert.Equal(4, transport.Width);
        Assert.Equal(1, transport.Height);
        Assert.Equal(
            [
                new Rgba32(10, 20, 30, 255),
                new Rgba32(40, 50, 60, 255),
                new Rgba32(0, 0, 0, 255),
                new Rgba32(128, 0, 0, 255)
            ],
            transport.Pixels);

        var bytes = await WebpTextureEncoder.EncodeRgbaDataLosslessAsync(
            transport.Pixels.ToArray(),
            transport.Width,
            transport.Height);
        using var decoded = Image.Load<Rgba32>(bytes);
        var actual = new Rgba32[transport.Pixels.Count];
        decoded.CopyPixelDataTo(actual);

        Assert.Equal(transport.Pixels, actual);
    }

    [Fact]
    public async Task Rgba_data_webp_preserves_color_channels_beneath_transparency()
    {
        var expected = new[]
        {
            new Rgba32(10, 20, 30, 0),
            new Rgba32(40, 50, 60, 128),
            new Rgba32(70, 80, 90, 255)
        };

        var bytes = await WebpTextureEncoder.EncodeRgbaDataLosslessAsync(expected, 3, 1);
        using var decoded = Image.Load<Rgba32>(bytes);
        var actual = new Rgba32[expected.Length];
        decoded.CopyPixelDataTo(actual);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Interlude_icon_package_can_be_read_when_local_source_is_available()
    {
        var root = FindRepositoryRoot();
        var source = Path.Combine(root, "sources", "Interlude", "systextures", "Icon.utx");
        if (!File.Exists(source))
        {
            return;
        }

        var encrypted = File.ReadAllBytes(source);
        var decoded = LineagePackageDecoder.DecodeProtocol121(encrypted, "Icon.utx");
        var textures = new UnrealPackageReader(decoded).ReadTextures();

        Assert.Contains(textures, texture =>
            texture.Name.EndsWith("skill0001", StringComparison.OrdinalIgnoreCase) &&
            texture.Width == 32 &&
            texture.Height == 32 &&
            texture.MipLevels.Count > 0);
    }

    [Fact]
    public void Interlude_static_mesh_package_can_be_read_when_local_source_is_available()
    {
        var root = FindRepositoryRoot();
        var sourceDirectory = Path.Combine(root, "sources", "Interlude", "staticmeshes");
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        var source = Directory.EnumerateFiles(sourceDirectory, "*.usx").Single(path =>
            string.Equals(Path.GetFileName(path), "v_obj_s.usx", StringComparison.OrdinalIgnoreCase));
        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var meshes = new UnrealPackageReader(decoded).ReadStaticMeshes();

        Assert.NotEmpty(meshes);
        Assert.Contains(meshes, mesh => mesh.Positions.Count > 0 && mesh.Indices.Count > 0);
        Assert.Contains(meshes, mesh =>
            mesh.VertexColors0.Count == mesh.Positions.Count ||
            mesh.VertexColors1.Count == mesh.Positions.Count);
        Assert.Contains(meshes.SelectMany(mesh => mesh.Sections), section => section.Material is not null);
        var glb = GlbStaticMeshEncoder.Encode(meshes.First(mesh => mesh.Positions.Count > 0 && mesh.Indices.Count > 0));
        Assert.Equal("glTF", System.Text.Encoding.ASCII.GetString(glb, 0, 4));
    }

    [Fact]
    public void Interlude_world_materials_can_be_read_when_local_source_is_available()
    {
        var root = FindRepositoryRoot();
        var fileName = "v_obj_t.utx";
        var source = Path.Combine(root, "sources", "Interlude", "textures", fileName);
        if (!File.Exists(source))
        {
            return;
        }

        var decoded = LineagePackageDecoder.DecodeProtocol121(File.ReadAllBytes(source), fileName);
        var materials = new UnrealPackageReader(decoded).ReadMaterialExports();

        Assert.NotEmpty(materials);
        Assert.All(materials, material => Assert.True(
            material.ClassName is "Shader" or "FinalBlend" or "Panner" or "Rotator" or "TexPanner" or "TexRotator" or "Combiner" or "TexOscillator" or "TexOscillatorTriggered" or "ColorModifier" or "FadeColor"));
        Assert.Contains(materials, material => material.Diffuse is not null || material.Material is not null);
    }

    [Theory]
    [InlineData("FX_E_T.utx")]
    public void Interlude_effect_material_graphs_can_be_read_when_local_source_is_available(string fileName)
    {
        var source = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", "textures", fileName);
        if (!File.Exists(source)) return;

        var decoded = LineagePackageDecoder.DecodeProtocol121(File.ReadAllBytes(source), fileName);
        var materials = new UnrealPackageReader(decoded).ReadMaterialExports();

        Assert.Contains(materials, material => material.ClassName is "Panner" or "Rotator" or "TexPanner" or "TexRotator" or "Combiner");
        Assert.Contains(materials, material => material.PanRate != 0 || material.RotationRate != 0);
        var oscillator = Assert.Single(materials, material => material.Name.EndsWith("WaterSurfaceShaderSet.TexOscillator0", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("TexOscillator", oscillator.ClassName);
        Assert.NotNull(oscillator.Material);
        Assert.Equal(0.1f, oscillator.UOscillationRate);
        Assert.Equal(0.05f, oscillator.UOscillationAmplitude);
        Assert.Contains(materials, material =>
            material.ClassName == "TexOscillatorTriggered");
        Assert.Contains(materials, material => material.ClassName == "FadeColor");
    }

    [Fact]
    public void Interlude_sky_color_modifiers_can_be_read_when_local_source_is_available()
    {
        const string fileName = "l2_skies.utx";
        var source = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", "textures", fileName);
        if (!File.Exists(source)) return;

        var decoded = LineagePackageDecoder.DecodeProtocol121(File.ReadAllBytes(source), fileName);
        var materials = new UnrealPackageReader(decoded).ReadMaterialExports();
        var modifier = Assert.Single(materials, material =>
            material.Name.EndsWith("Shaders.SkybackgroundColor", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("ColorModifier", modifier.ClassName);
        Assert.NotNull(modifier.Material);
        Assert.Equal(new UnrealColor(0, 150, 206, 255), modifier.ModifierColor);
    }

    [Fact]
    public void Interlude_flame_flipbook_chain_can_be_read_when_local_source_is_available()
    {
        const string fileName = "FX_E_T.utx";
        var source = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", "textures", fileName);
        if (!File.Exists(source)) return;

        var decoded = LineagePackageDecoder.DecodeProtocol121(File.ReadAllBytes(source), fileName);
        var exports = new UnrealPackageReader(decoded).ReadTextureExports();
        var first = Assert.Single(exports, export => export.Name.EndsWith("de_fire_0000", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(first.AnimationNext);
        Assert.EndsWith("de_fire_0001", first.AnimationNext!.ObjectName, StringComparison.OrdinalIgnoreCase);
        Assert.True(first.MinFrameRate > 0 || first.MaxFrameRate > 0);
    }

    [Fact]
    public void Interlude_ambient_sounds_can_be_read_when_local_source_is_available()
    {
        var source = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", "sounds", "ambsound.uax");
        if (!File.Exists(source)) return;

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var sounds = new UnrealPackageReader(decoded).ReadSoundExports();

        Assert.NotEmpty(sounds);
        Assert.Contains(sounds, sound => sound.Name.EndsWith("Fire.fire_02", StringComparison.OrdinalIgnoreCase));
        Assert.All(sounds, sound =>
        {
            Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(sound.WaveData, 0, 4));
            Assert.True(sound.DurationSeconds > 0);
            Assert.True(sound.SampleRate > 0);
            Assert.True(sound.Channels > 0);
        });
    }

    [Fact]
    public void Interlude_static_mesh_packages_can_be_inspected_when_explicitly_requested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("L2_INSPECT_STATIC_MESHES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var sourceDirectory = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", "staticmeshes");
        var files = Directory.GetFiles(sourceDirectory, "*.usx").OrderBy(path => path).ToArray();
        Assert.NotEmpty(files);
        foreach (var path in files)
        {
            try
            {
                var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(path));
                var meshes = new UnrealPackageReader(decoded).ReadStaticMeshes();
                Console.WriteLine($"{Path.GetFileName(path)}\tmeshes={meshes.Count}");
            }
            catch (Exception exception)
            {
                throw new InvalidDataException($"Package '{Path.GetFileName(path)}' could not be inspected.", exception);
            }
        }
    }

    [Theory]
    [InlineData("L2Font.utx")]
    [InlineData("symbol.utx")]
    public void Interlude_empty_texture_exports_are_zero_mip_placeholders(string fileName)
    {
        var root = FindRepositoryRoot();
        var source = Path.Combine(root, "sources", "Interlude", "systextures", fileName);
        if (!File.Exists(source))
        {
            return;
        }

        var encrypted = File.ReadAllBytes(source);
        var decoded = LineagePackageDecoder.DecodeProtocol121(encrypted, fileName);
        var emptyExports = new UnrealPackageReader(decoded)
            .ReadTextureExports()
            .Where(texture => texture.Texture is null)
            .ToArray();

        Assert.NotEmpty(emptyExports);
        Assert.All(emptyExports, texture => Assert.Equal(0, texture.MipCount));
    }

    [Fact]
    public void Interlude_systextures_can_be_inspected_when_explicitly_requested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("L2_INSPECT_SYSTEXTURES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        InspectTextureDirectory("systextures");
    }

    [Fact]
    public void Interlude_textures_can_be_inspected_when_explicitly_requested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("L2_INSPECT_TEXTURES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        InspectTextureDirectory("textures");
    }

    private static void InspectTextureDirectory(string directoryName)
    {
        var sourceDirectory = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", directoryName);
        var files = Directory.GetFiles(sourceDirectory, "*.utx").OrderBy(path => path).ToArray();
        Assert.NotEmpty(files);
        foreach (var path in files)
        {
            var fileName = Path.GetFileName(path);
            IReadOnlyList<UnrealTextureExport> exports;
            try
            {
                var encrypted = File.ReadAllBytes(path);
                var decoded = LineagePackageDecoder.DecodeProtocol121(encrypted, fileName);
                exports = new UnrealPackageReader(decoded).ReadTextureExports();
            }
            catch (Exception exception)
            {
                throw new InvalidDataException($"Package '{fileName}' could not be inspected.", exception);
            }
            Assert.NotEmpty(exports);
            foreach (var texture in exports
                .Select(export => export.Texture)
                .Where(texture => texture is not null && KtxTextureEncoder.CanEncode(texture)))
            {
                Assert.NotEmpty(KtxTextureEncoder.Encode(texture!));
            }
            var formats = string.Join(
                ",",
                exports.GroupBy(texture => texture.Format)
                    .OrderBy(group => group.Key)
                    .Select(group => $"{group.Key?.ToString() ?? "none"}:{group.Count()}"));
            Console.WriteLine($"{fileName}\ttextures={exports.Count}\tformats={formats}");
            foreach (var texture in exports.Where(texture => texture.Format is null).Take(3))
            {
                Console.WriteLine($"  no-format\t{texture.Name}\t{texture.Width}x{texture.Height}");
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "L2Web.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }

    private static uint ReadUInt32(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
}

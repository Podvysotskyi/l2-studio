using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;
using L2.Tools.PackageReader;

namespace L2.Tools.StaticMeshConverter;

public static class GlbStaticMeshEncoder
{
    private const uint GlbMagic = 0x46546c67;
    private const uint JsonChunkType = 0x4e4f534a;
    private const uint BinaryChunkType = 0x004e4942;

    public static byte[] Encode(
        UnrealStaticMesh mesh,
        IReadOnlyList<StaticMeshMaterialBinding?>? sectionMaterials = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.Positions.Count == 0 || mesh.Indices.Count == 0)
        {
            throw new InvalidDataException($"Static mesh '{mesh.Name}' has no renderable geometry.");
        }

        using var binary = new MemoryStream();
        var positionOffset = (int)binary.Position;
        foreach (var position in mesh.Positions)
        {
            WriteVector3(binary, ConvertVector(position));
        }

        var normalOffset = (int)binary.Position;
        foreach (var normal in mesh.Normals)
        {
            WriteVector3(binary, ConvertVector(normal));
        }

        var uvOffset = (int)binary.Position;
        var hasTextureCoordinates = mesh.TextureCoordinates.Count == mesh.Positions.Count;
        if (hasTextureCoordinates)
        {
            foreach (var coordinate in mesh.TextureCoordinates)
            {
                WriteSingle(binary, coordinate.X);
                WriteSingle(binary, coordinate.Y);
            }
        }

        var hasColorStream0 = mesh.VertexColors0.Count == mesh.Positions.Count;
        var color0Offset = (int)binary.Position;
        if (hasColorStream0)
        {
            foreach (var color in mesh.VertexColors0) WriteColor(binary, color);
        }
        var hasColorStream1 = mesh.VertexColors1.Count == mesh.Positions.Count;
        var color1Offset = (int)binary.Position;
        if (hasColorStream1)
        {
            foreach (var color in mesh.VertexColors1) WriteColor(binary, color);
        }

        var indexOffset = (int)binary.Position;
        Span<byte> indexBytes = stackalloc byte[2];
        foreach (var index in mesh.Indices)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(indexBytes, index);
            binary.Write(indexBytes);
        }
        Pad(binary, 4, 0);

        var convertedPositions = mesh.Positions.Select(ConvertVector).ToArray();
        var minimum = new[]
        {
            convertedPositions.Min(value => value.X),
            convertedPositions.Min(value => value.Y),
            convertedPositions.Min(value => value.Z)
        };
        var maximum = new[]
        {
            convertedPositions.Max(value => value.X),
            convertedPositions.Max(value => value.Y),
            convertedPositions.Max(value => value.Z)
        };
        var bufferViews = new List<object>
        {
            new { buffer = 0, byteOffset = positionOffset, byteLength = mesh.Positions.Count * 12, target = 34962 },
            new { buffer = 0, byteOffset = normalOffset, byteLength = mesh.Normals.Count * 12, target = 34962 }
        };
        var positionAccessor = 0;
        var normalAccessor = 1;
        int? uvAccessor = null;
        if (hasTextureCoordinates)
        {
            bufferViews.Add(new { buffer = 0, byteOffset = uvOffset, byteLength = mesh.TextureCoordinates.Count * 8, target = 34962 });
            uvAccessor = 2;
        }
        int? color0Accessor = null;
        if (hasColorStream0)
        {
            var view = bufferViews.Count;
            bufferViews.Add(new { buffer = 0, byteOffset = color0Offset, byteLength = mesh.VertexColors0.Count * 4, target = 34962 });
            color0Accessor = hasTextureCoordinates ? 3 : 2;
        }
        int? color1Accessor = null;
        if (hasColorStream1)
        {
            var view = bufferViews.Count;
            bufferViews.Add(new { buffer = 0, byteOffset = color1Offset, byteLength = mesh.VertexColors1.Count * 4, target = 34962 });
            color1Accessor = (hasTextureCoordinates ? 3 : 2) + (hasColorStream0 ? 1 : 0);
        }
        var indexBufferView = bufferViews.Count;
        bufferViews.Add(new { buffer = 0, byteOffset = indexOffset, byteLength = mesh.Indices.Count * 2, target = 34963 });
        var indexAccessor = 2 + (hasTextureCoordinates ? 1 : 0) + (hasColorStream0 ? 1 : 0) + (hasColorStream1 ? 1 : 0);
        var accessors = new List<object>
        {
            new { bufferView = 0, componentType = 5126, count = mesh.Positions.Count, type = "VEC3", min = minimum, max = maximum },
            new { bufferView = 1, componentType = 5126, count = mesh.Normals.Count, type = "VEC3" }
        };
        if (hasTextureCoordinates)
        {
            accessors.Add(new { bufferView = 2, componentType = 5126, count = mesh.TextureCoordinates.Count, type = "VEC2" });
        }
        if (hasColorStream0)
        {
            var bufferView = 2 + (hasTextureCoordinates ? 1 : 0);
            accessors.Add(new { bufferView, componentType = 5121, normalized = true, count = mesh.VertexColors0.Count, type = "VEC4" });
        }
        if (hasColorStream1)
        {
            var bufferView = 2 + (hasTextureCoordinates ? 1 : 0) + (hasColorStream0 ? 1 : 0);
            accessors.Add(new { bufferView, componentType = 5121, normalized = true, count = mesh.VertexColors1.Count, type = "VEC4" });
        }
        var validSections = mesh.Sections
            .Select((section, index) => (Section: section, Index: index))
            .Where(item =>
                item.Section.FirstIndex >= 0 &&
                item.Section.IndexCount > 0 &&
                item.Section.FirstIndex + item.Section.IndexCount <= mesh.Indices.Count)
            .ToArray();
        var sectionIndexAccessors = new List<int>();
        if (validSections.Length == 0)
        {
            accessors.Add(new { bufferView = indexBufferView, byteOffset = 0, componentType = 5123, count = mesh.Indices.Count, type = "SCALAR" });
            sectionIndexAccessors.Add(indexAccessor);
        }
        else
        {
            foreach (var item in validSections)
            {
                var section = item.Section;
                sectionIndexAccessors.Add(accessors.Count);
                accessors.Add(new
                {
                    bufferView = indexBufferView,
                    byteOffset = section.FirstIndex * 2,
                    componentType = 5123,
                    count = section.IndexCount,
                    type = "SCALAR"
                });
            }
        }

        var attributes = new Dictionary<string, int>
        {
            ["POSITION"] = positionAccessor,
            ["NORMAL"] = normalAccessor
        };
        if (uvAccessor is not null)
        {
            attributes["TEXCOORD_0"] = uvAccessor.Value;
        }
        if (color0Accessor is not null) attributes["COLOR_0"] = color0Accessor.Value;
        if (color1Accessor is not null) attributes["COLOR_1"] = color1Accessor.Value;

        var images = new List<object>();
        var textures = new List<object>();
        var textureIndices = new Dictionary<string, int>(StringComparer.Ordinal);
        int TextureIndex(string url)
        {
            if (textureIndices.TryGetValue(url, out var existing))
            {
                return existing;
            }
            var index = textures.Count;
            images.Add(new { uri = ImageUri(url) });
            textures.Add(new { sampler = 0, source = index });
            textureIndices[url] = index;
            return index;
        }

        var materials = new List<object>();
        int MaterialIndex(StaticMeshMaterialBinding binding)
        {
            var pbr = new Dictionary<string, object?>
            {
                ["baseColorFactor"] = binding.BlendMode == StaticMeshBlendMode.Invisible
                    ? new[] { binding.Tint?.R ?? 1f, binding.Tint?.G ?? 1f, binding.Tint?.B ?? 1f, 0f }
                    : new[] { binding.Tint?.R ?? 1f, binding.Tint?.G ?? 1f, binding.Tint?.B ?? 1f, binding.Tint?.A ?? 1f },
                ["metallicFactor"] = 0f,
                ["roughnessFactor"] = 1f
            };
            if (binding.DiffuseUrl is not null)
            {
                pbr["baseColorTexture"] = new { index = TextureIndex(binding.DiffuseUrl) };
            }
            var material = new Dictionary<string, object?>
            {
                ["name"] = binding.Name,
                ["pbrMetallicRoughness"] = pbr,
                ["doubleSided"] = binding.DoubleSided,
                ["extras"] = new
                {
                    l2 = new
                    {
                        blendMode = binding.BlendMode.ToString().ToLowerInvariant(),
                        opacityUrl = binding.OpacityUrl,
                        opacitySource = binding.OpacitySource.ToString().ToLowerInvariant(),
                        opacityChannel = binding.OpacityChannel.ToString().ToLowerInvariant(),
                        unlit = binding.Unlit,
                        depthWrite = binding.DepthWrite,
                        depthTest = binding.DepthTest,
                        panRate = binding.PanRate,
                        panRateV = binding.PanRateV,
                        rotationRate = binding.RotationRate,
                        detailUrl = binding.DetailUrl,
                        detailScale = binding.DetailScale,
                        diffuseAnimation = Animation(binding.DiffuseAnimation),
                        opacityAnimation = Animation(binding.OpacityAnimation),
                        emissiveAnimation = Animation(binding.EmissiveAnimation),
                        uvOscillation = binding.UvOscillation,
                        fade = Fade(binding.Fade),
                        composite = Composite(binding.Composite),
                        selfIlluminationMaskUrl = binding.SelfIlluminationMaskUrl,
                        specularUrl = binding.SpecularUrl,
                        specularityMaskUrl = binding.SpecularityMaskUrl,
                        performLightingOnSpecularPass = binding.PerformLightingOnSpecularPass,
                        windMode = binding.WindMode == StaticMeshWindMode.None
                            ? null
                            : binding.WindMode.ToString().ToLowerInvariant()
                    }
                }
            };
            if (binding.EmissiveUrl is not null)
            {
                material["emissiveTexture"] = new { index = TextureIndex(binding.EmissiveUrl) };
                material["emissiveFactor"] = new[] { 1f, 1f, 1f };
            }
            if (binding.BlendMode == StaticMeshBlendMode.Masked)
            {
                material["alphaMode"] = "MASK";
                material["alphaCutoff"] = binding.AlphaCutoff;
            }
            else if (binding.BlendMode != StaticMeshBlendMode.Opaque)
            {
                material["alphaMode"] = "BLEND";
            }
            materials.Add(material);
            return materials.Count - 1;
        }

        static object? Animation(StaticMeshTextureAnimation? animation) => animation is null
            ? null
            : new { frameUrls = animation.FrameUrls, frameRate = animation.FrameRate };
        static object? Tint(StaticMeshMaterialTint? tint) => tint is null
            ? null
            : new { r = tint.R, g = tint.G, b = tint.B, a = tint.A };
        static object? Fade(StaticMeshMaterialFade? fade) => fade is null
            ? null
            : new
            {
                color1 = Tint(fade.Color1),
                color2 = Tint(fade.Color2),
                type = fade.Type,
                period = fade.Period,
                phase = fade.Phase
            };
        static object? Composite(StaticMeshMaterialComposite? composite) => composite is null
            ? null
            : new
            {
                secondaryUrl = composite.SecondaryUrl,
                secondaryTint = Tint(composite.SecondaryTint),
                secondaryFade = Fade(composite.SecondaryFade),
                maskUrl = composite.MaskUrl,
                colorOperation = composite.ColorOperation,
                alphaOperation = composite.AlphaOperation,
                invertMask = composite.InvertMask,
                modulateScale = composite.ModulateScale
            };

        var primitives = new List<object>();
        for (var index = 0; index < sectionIndexAccessors.Count; index++)
        {
            StaticMeshMaterialBinding? binding = null;
            if (validSections.Length > 0 && sectionMaterials is not null)
            {
                var sectionIndex = validSections[index].Index;
                if (sectionIndex < sectionMaterials.Count)
                {
                    binding = sectionMaterials[sectionIndex];
                }
            }
            var primitive = new Dictionary<string, object>
            {
                ["attributes"] = attributes,
                ["indices"] = sectionIndexAccessors[index]
            };
            if (binding is not null)
            {
                primitive["material"] = MaterialIndex(binding);
            }
            primitives.Add(primitive);
        }

        var document = new Dictionary<string, object?>
        {
            ["asset"] = new { version = "2.0", generator = "L2.Tools.StaticMeshConverter" },
            ["scene"] = 0,
            ["scenes"] = new[] { new { nodes = new[] { 0 } } },
            ["nodes"] = new[] { new { mesh = 0, name = mesh.Name } },
            ["meshes"] = new[]
            {
                new
                {
                    name = mesh.Name,
                    primitives
                }
            },
            ["buffers"] = new[] { new { byteLength = (int)binary.Length } },
            ["bufferViews"] = bufferViews,
            ["accessors"] = accessors
        };
        if (materials.Count > 0)
        {
            document["materials"] = materials;
            document["samplers"] = new[] { new { magFilter = 9729, minFilter = 9987, wrapS = 10497, wrapT = 10497 } };
            document["images"] = images;
            document["textures"] = textures;
        }
        var json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(document));

        using var output = new MemoryStream();
        var paddedJsonLength = Align(json.Length, 4);
        var totalLength = 12 + 8 + paddedJsonLength + 8 + (int)binary.Length;
        WriteUInt32(output, GlbMagic);
        WriteUInt32(output, 2);
        WriteUInt32(output, (uint)totalLength);
        WriteUInt32(output, (uint)paddedJsonLength);
        WriteUInt32(output, JsonChunkType);
        output.Write(json);
        Pad(output, 4, 0x20);
        WriteUInt32(output, (uint)binary.Length);
        WriteUInt32(output, BinaryChunkType);
        binary.Position = 0;
        binary.CopyTo(output);
        return output.ToArray();
    }

    private static string ImageUri(string url) => url.StartsWith("/", StringComparison.Ordinal)
        ? url.TrimStart('/')
        : url;

    // Unreal is left-handed with Z up; glTF is right-handed with Y up. Swapping
    // Y and Z has a negative determinant, which also converts Unreal's clockwise
    // front-face winding into glTF's counterclockwise winding.
    private static Vector3 ConvertVector(Vector3 value) => new(value.X, value.Z, value.Y);

    private static void WriteVector3(Stream stream, Vector3 value)
    {
        WriteSingle(stream, value.X);
        WriteSingle(stream, value.Y);
        WriteSingle(stream, value.Z);
    }

    private static void WriteColor(Stream stream, UnrealColor color)
    {
        stream.WriteByte(color.Red);
        stream.WriteByte(color.Green);
        stream.WriteByte(color.Blue);
        stream.WriteByte(color.Alpha);
    }

    private static void WriteSingle(Stream stream, float value) =>
        WriteUInt32(stream, unchecked((uint)BitConverter.SingleToInt32Bits(value)));

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void Pad(Stream stream, int alignment, byte value)
    {
        while (stream.Position % alignment != 0)
        {
            stream.WriteByte(value);
        }
    }

    private static int Align(int value, int alignment) => (value + alignment - 1) / alignment * alignment;
}
